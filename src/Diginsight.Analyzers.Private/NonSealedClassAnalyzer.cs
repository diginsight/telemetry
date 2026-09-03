using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Diginsight.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonSealedClassAnalyzer : DiagnosticAnalyzer
{
    public const string MissingDiagnosticId = "DIGPRV001";
    public const string ConflictDiagnosticId = "DIGPRV002";

    private static readonly string AttributeMetadataName = $"{NonSealedAttributeGenerator.AttributeNamespace}.{NonSealedAttributeGenerator.AttributeName}Attribute";

    private static readonly DiagnosticDescriptor MissingRule = new (
        MissingDiagnosticId,
        title: "Class hierarchy openness must be explicit",
        messageFormat: "Class '{0}' is neither sealed nor marked with [NonSealed]",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: $"Non-static classes should be sealed or explicitly annotated with [{NonSealedAttributeGenerator.AttributeName}] to make open hierarchies intentional."
    );

    private static readonly DiagnosticDescriptor ConflictRule = new (
        ConflictDiagnosticId,
        title: "Class hierarchy openness is contradictory",
        messageFormat: "Class '{0}' is both sealed and marked with [NonSealed]",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: $"A sealed class must not also be annotated with [{NonSealedAttributeGenerator.AttributeName}]; the two are mutually exclusive."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [ MissingRule, ConflictRule ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(
            static compilationStartContext =>
            {
                INamedTypeSymbol? attributeSymbol =
                    compilationStartContext.Compilation.GetTypeByMetadataName(AttributeMetadataName);

                compilationStartContext.RegisterSymbolAction(
                    symbolContext => AnalyzeNamedType(symbolContext, attributeSymbol),
                    SymbolKind.NamedType
                );
            }
        );
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol? attributeSymbol)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

        if (type is not { TypeKind: TypeKind.Class, IsStatic: false, IsImplicitlyDeclared: false })
        {
            return;
        }

        bool isMarkedNonSealed = type.GetAttributes().Any(a => IsNonSealedAttribute(a, attributeSymbol));

        if (type.IsSealed)
        {
            if (isMarkedNonSealed)
            {
                ReportConflict(context, type, attributeSymbol);
            }

            return;
        }

        if (!type.IsAbstract && !isMarkedNonSealed && type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) is { } location)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingRule, location, type.Name));
        }
    }

    private static void ReportConflict(SymbolAnalysisContext context, ISymbol type, INamedTypeSymbol? attributeSymbol)
    {
        foreach (SyntaxReference syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                continue;

            SyntaxToken sealedToken = typeDeclaration.Modifiers.FirstOrDefault(static modifier => modifier.IsKind(SyntaxKind.SealedKeyword));
            if (!sealedToken.IsKind(SyntaxKind.None))
            {
                context.ReportDiagnostic(Diagnostic.Create(ConflictRule, sealedToken.GetLocation(), type.Name));
            }
        }

        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (!IsNonSealedAttribute(attribute, attributeSymbol) ||
                attribute.ApplicationSyntaxReference is not { } applicationReference)
                continue;

            Location location = Location.Create(applicationReference.SyntaxTree, applicationReference.Span);
            context.ReportDiagnostic(Diagnostic.Create(ConflictRule, location, type.Name));
        }
    }

    private static bool IsNonSealedAttribute(AttributeData attribute, INamedTypeSymbol? attributeSymbol)
    {
        if (attribute.AttributeClass is not { } attributeClass)
            return false;

        if (attributeSymbol is not null &&
            SymbolEqualityComparer.Default.Equals(attributeClass, attributeSymbol))
            return true;

        return attributeClass.ToDisplayString() == AttributeMetadataName;
    }
}
