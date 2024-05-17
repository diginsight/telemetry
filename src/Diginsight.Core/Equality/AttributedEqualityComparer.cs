using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Diginsight.Equality;

public sealed class AttributedEqualityComparer : IEqualityComparer<object>
{
    public static readonly IEqualityComparer<object> Instance = new AttributedEqualityComparer();

    private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Type, EquatorTemplate>> cachedEquatorTemplatesDicts = new ();

    private AttributedEqualityComparer() { }

    public bool Equals(object? obj1, object? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }
        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        EqualityMode GetEqualityMode(IEquatableDescriptor? descriptor) => descriptor?.Mode ?? EqualityMode.Default;

        IEqualityTypeContract? typeContract = GetContract(obj1.GetType());
        EqualityMode mode = GetEqualityMode(typeContract);
        if (mode != GetEqualityMode(GetContract(obj2.GetType())))
        {
            throw new ArgumentException("Equality modes are different");
        }

        if (mode == EqualityMode.Reference)
        {
            return false;
        }

        if (TryByEquatable(obj1, obj2, mode))
        {
            return true;
        }

        if (mode == EqualityMode.Default)
        {
            return EqualityComparer<object>.Default.Equals(obj1, obj2);
        }

        throw new NotImplementedException();
    }

    private IEqualityTypeContract GetContract(Type objType)
    {
        throw new NotImplementedException();
    }

    private bool TryByEquatable(object obj1, object obj2, EqualityMode mode)
    {
        IReadOnlyDictionary<Type, EquatorTemplate> templates = GetEquatorTemplates(obj1.GetType());
        if (!(templates.Count > 0))
        {
            return false;
        }
        if (mode != EqualityMode.Default)
        {
            throw new ArgumentException($"Object implements {nameof(IEquatable<object>)}<> but equality mode is not {nameof(EqualityMode.Default)}", nameof(obj1));
        }

        return templates.Any(x => x.Key.IsInstanceOfType(obj2) && x.Value.CreateEquator(obj1)(obj2));
    }

    private IReadOnlyDictionary<Type, EquatorTemplate> GetEquatorTemplates(Type objType)
    {
        static IReadOnlyDictionary<Type, EquatorTemplate> CoreGetEquatorTemplates(Type objType)
        {
            return objType.GetGenericArgumentsAs(typeof(IEquatable<>))
                .Select(static argTypes => argTypes[0])
                .ToDictionary(
                    static argType => argType, static argType =>
                    {
                        Type equatableType = typeof(IEquatable<>).MakeGenericType(argType);
                        ParameterExpression parameterExpr = Expression.Parameter(typeof(object));
                        ParameterExpression placeholderExpr = Expression.Parameter(equatableType);

                        Expression<Func<object, bool>> lambdaExpr = Expression.Lambda<Func<object, bool>>(
                            Expression.Call(
                                placeholderExpr,
                                equatableType.GetMethod(nameof(IEquatable<object>.Equals))!,
                                Expression.Convert(parameterExpr, argType)
                            ),
                            parameterExpr
                        );

                        return new EquatorTemplate(lambdaExpr, placeholderExpr);
                    }
                );
        }

        return cachedEquatorTemplatesDicts.GetOrAdd(objType, CoreGetEquatorTemplates);
    }

    private sealed class EquatorTemplate
    {
        private readonly Expression<Func<object, bool>> lambda;
        private readonly Expression placeholder;

        public EquatorTemplate(Expression<Func<object, bool>> lambda, Expression placeholder)
        {
            this.lambda = lambda;
            this.placeholder = placeholder;
        }

        public Func<object, bool> CreateEquator(object instance)
        {
            return ((Expression<Func<object, bool>>)new Replacer(placeholder, instance).Visit(lambda)!).Compile();
        }

        private sealed class Replacer : ExpressionVisitor
        {
            private readonly Expression placeholder;
            private readonly object instance;

            public Replacer(Expression placeholder, object instance)
            {
                this.placeholder = placeholder;
                this.instance = instance;
            }

            public override Expression? Visit(Expression? node)
            {
                return node == placeholder ? Expression.Constant(instance) : base.Visit(node);
            }
        }
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
