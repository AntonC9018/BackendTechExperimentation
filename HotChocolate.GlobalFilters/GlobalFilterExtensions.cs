using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Types;

namespace HotChocolate.GlobalFilters;

public static class GlobalFilterExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="descriptor"></param>
    /// <param name="filterExpression">
    /// The expression that shall be applied to all nested properties of the given type
    /// </param>
    public static IObjectTypeDescriptor<T> GlobalFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, bool>> filterExpression)
        where T : class
    {
        // We have to wrap it. 
        var filter = new ExpressionGlobalFilter(filterExpression);
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.FilterKey] = filter);
        return descriptor;
    }
    
    /// <summary>
    /// </summary>
    public static IObjectTypeDescriptor<T> GlobalFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        IGlobalFilter<T> filter)
    {
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.FilterKey] = filter);
        return descriptor;
    }

    /// <summary>
    /// </summary>
    public static IObjectTypeDescriptor<T> GlobalFilterIgnoreCondition<T>(
        this IObjectTypeDescriptor<T> descriptor,
        IIgnoreCondition ignoreCondition)
    {
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.IgnoreKey] = ignoreCondition);
        return descriptor;
    }

    /// <summary>
    /// Adds a middleware that shall apply the global filter of the type of the query
    /// to the root of the query.
    /// </summary>
    /// <param name="descriptor"></param>
    public static IObjectFieldDescriptor UseGlobalFilter(this IObjectFieldDescriptor descriptor)
    {
        descriptor.Use<GlobalFilterApplicationMiddleware>();
        return descriptor;
    }

    /// <summary>
    /// Adds a global filter which allows the expression to be curried
    /// with a value extracted from the context at runtime.
    /// </summary>
    /// <param name="descriptor"></param>
    /// <param name="contextExtractor">
    /// The object that shall provide the value for the second parameter of the expression at runtime
    /// </param>
    /// <param name="filterExpression">The expression that represents the filter</param>
    public static IObjectTypeDescriptor<T> GlobalFilter<T, TContext>(
        this IObjectTypeDescriptor<T> descriptor,
        IValueExtractor<TContext> contextExtractor,
        Expression<Func<T, TContext, bool>> filterExpression)
    
        where T : class
    {
        var filter = new GlobalFilterWithContext<T, TContext>(filterExpression, contextExtractor);
        descriptor.GlobalFilter(filter);
        return descriptor;
    }

    /// <summary>
    /// Adds the global filter projection handlers to the projection provider.
    /// These handlers will allow applying, in effect, row-level security to all
    /// nested fields of a query, automatically.
    /// </summary>
    public static IProjectionProviderDescriptor AddGlobalFilterHandlers(
        this IProjectionProviderDescriptor descriptor)
    {
        descriptor.RegisterFieldHandler<GlobalFilterQueryableProjectionListHandler>();
        descriptor.RegisterFieldHandler<GlobalFilterQueryableProjectionFieldHandler>();
        return descriptor;
    } 
}