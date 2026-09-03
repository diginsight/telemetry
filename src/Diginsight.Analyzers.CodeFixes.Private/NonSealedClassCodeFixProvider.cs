using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;

namespace Diginsight.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonSealedClassCodeFixProvider))]
[Shared]
public sealed class NonSealedClassCodeFixProvider : CodeFixProvider
{
    private const string AttributeName = NonSealedAttributeGenerator.AttributeName;
    private static readonly string AttributeNamespace = NonSealedAttributeGenerator.AttributeNamespace;
    private static readonly string AttributeMetadataName = $"{AttributeNamespace}.{AttributeName}Attribute";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ NonSealedClassAnalyzer.MissingDiagnosticId, NonSealedClassAnalyzer.ConflictDiagnosticId ];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not { } root)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan) is not { } node ||
                node.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } classDeclaration)
                continue;

            if (diagnostic.Id == NonSealedClassAnalyzer.ConflictDiagnosticId)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove 'sealed' modifier",
                        ct => RemoveSealedAsync(context.Document, classDeclaration, ct),
                        "RemoveSealed"
                    ),
                    diagnostic
                );

                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Remove [{AttributeName}] attribute",
                        ct => RemoveNonSealedAttributeAsync(context.Document, classDeclaration, ct),
                        "RemoveNonSealed"
                    ),
                    diagnostic
                );

                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Make class sealed",
                    ct => MakeSealedAsync(context.Document, classDeclaration, ct),
                    "MakeSealed"
                ),
                diagnostic
            );

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Mark class with [{AttributeName}]",
                    ct => MarkNonSealedAsync(context.Document, classDeclaration, ct),
                    "MarkNonSealed"
                ),
                diagnostic
            );
        }
    }

    private static async Task<Document> MakeSealedAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        SyntaxGenerator generator = editor.Generator;

        DeclarationModifiers modifiers = generator.GetModifiers(classDeclaration);
        SyntaxNode newClass = generator.WithModifiers(classDeclaration, modifiers.WithIsSealed(true));
        editor.ReplaceNode(classDeclaration, newClass);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveSealedAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        SyntaxGenerator generator = editor.Generator;

        DeclarationModifiers modifiers = generator.GetModifiers(classDeclaration);
        SyntaxNode newClass = generator.WithModifiers(classDeclaration, modifiers.WithIsSealed(false));
        editor.ReplaceNode(classDeclaration, newClass);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveNonSealedAttributeAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);

        AttributeSyntax? attribute = classDeclaration.AttributeLists
            .SelectMany(static list => list.Attributes)
            .FirstOrDefault(candidate => IsNonSealedAttribute(candidate, semanticModel, cancellationToken));

        if (attribute?.Parent is not AttributeListSyntax attributeList)
        {
            return document;
        }

        if (attributeList.Attributes.Count == 1)
        {
            editor.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            editor.RemoveNode(attribute);
        }

        return editor.GetChangedDocument();
    }

    private static bool IsNonSealedAttribute(AttributeSyntax attribute, SemanticModel? semanticModel, CancellationToken cancellationToken)
    {
        if (semanticModel?.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingType is { } attributeType)
            return attributeType.ToDisplayString() == AttributeMetadataName;

        string name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            _ => attribute.Name.ToString(),
        };

        return name is AttributeName or $"{AttributeName}Attribute";
    }

    private static async Task<Document> MarkNonSealedAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        SyntaxGenerator generator = editor.Generator;

        SyntaxNode attribute = generator.Attribute(AttributeName).WithAdditionalAnnotations(Formatter.Annotation);
        editor.AddAttribute(classDeclaration, attribute);

        Document changedDocument = editor.GetChangedDocument();
        return await EnsureUsingAsync(changedDocument, AttributeNamespace, cancellationToken);
    }

    private static async Task<Document> EnsureUsingAsync(Document document, string @namespace, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
        {
            return document;
        }

        bool alreadyImported = root
            .DescendantNodesAndSelf()
            .OfType<UsingDirectiveSyntax>()
            .Any(ud => ud.Alias is null && ud.StaticKeyword.IsKind(SyntaxKind.None) && ud.Name!.ToString() == @namespace);

        if (alreadyImported)
            return document;

        UsingDirectiveSyntax usingDirective = SyntaxFactory
            .UsingDirective(SyntaxFactory.ParseName(@namespace))
            .WithAdditionalAnnotations(Formatter.Annotation);

        int index = root.Usings.LastIndexOf(
            ud => ud.Alias is null
                && ud.StaticKeyword.IsKind(SyntaxKind.None)
                && ud.Name!.ToString().CompareTo(@namespace, StringComparison.Ordinal) < 0
        );

        SyntaxList<UsingDirectiveSyntax> newUsings = root.Usings.Insert(index + 1, usingDirective);
        CompilationUnitSyntax newRoot = root.WithUsings(newUsings);
        return document.WithSyntaxRoot(newRoot);
    }
}
