using System.Linq.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.GlobalFilters;

/// <summary>
/// A global filter implementation that substitutes the value of the second parameter
/// of the given expression for one extracted from the context.
/// Should be enough for most cases.
/// </summary>
/// <typeparam name="T">Runtime field type</typeparam>
/// <typeparam name="TContext">The type of the curried value</typeparam>
public sealed class GlobalFilterWithContext<T, TContext> : IGlobalFilter<T>
{
    private readonly Expression<Func<T, TContext, bool>> _filterExpression;
    private readonly IValueExtractor<TContext> _valueExtractor;

    public GlobalFilterWithContext(
        Expression<Func<T, TContext, bool>> filterExpression,
        IValueExtractor<TContext> valueExtractor)
    {
        _filterExpression = filterExpression;
        _valueExtractor = valueExtractor;
    }
    
    public Expression<Func<T, bool>> GetFilterT(IResolverContext context)
    {
        var value = _valueExtractor.GetValue(context);
        var lambda = GlobalFilterHelper.CurrySecondParameter(_filterExpression, value);
        return lambda;    
    }

    public LambdaExpression GetFilter(IResolverContext context)
    {
        return GetFilterT(context);
    }
}

/// <summary>
/// Used to extract a value to be substituted in expressions from the context.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IValueExtractor<out TValue>
{
    TValue GetValue(IResolverContext context);
}

public static class ValueExtractor
{
    /// <summary>
    /// Wraps the provider delegate in a <see cref="ValueExtractor{T}"/>.
    /// </summary>
    public static ValueExtractor<T> Create<T>(Func<IResolverContext, T> getter)
    {
        return new ValueExtractor<T>(getter);
    }        
}

/// <summary>
/// Represents a wrapped provider delegate
/// used to extract a value from the context at runtime.
/// </summary>
/// <typeparam name="T">The type of the value that shall be extracted.</typeparam>
public sealed class ValueExtractor<T> : IValueExtractor<T>
{
    private readonly Func<IResolverContext, T> _getter;
    
    public ValueExtractor(Func<IResolverContext, T> getter)
    {
        _getter = getter;
    }
    
    public T GetValue(IResolverContext context)
    {
        return _getter(context);
    }
}

// NOTE: might not work because of concurrency issues. Don't use yet.
internal sealed class CachingGlobalFilterWrapper : IGlobalFilter
{
    // private const string ExpressionCacheKey = "GlobalFilterExpression";
    private readonly IGlobalFilter _underlyingFilter;
    private readonly string _uniqueKey;

    public CachingGlobalFilterWrapper(IGlobalFilter underlyingFilter, string uniqueKey)
    {
        _underlyingFilter = underlyingFilter;
        _uniqueKey = uniqueKey;
    }

    public LambdaExpression GetFilter(IResolverContext context)
    {
        if (!context.ContextData.TryGetValue(_uniqueKey, out var result))
        {
            result = _underlyingFilter.GetFilter(context);
            context.ContextData.Add(_uniqueKey, result);
        }
        return (LambdaExpression) result!;
    }
}
