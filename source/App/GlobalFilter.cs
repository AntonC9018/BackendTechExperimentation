using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace efcore_transactions;

public static class GlobalFilterConstants
{
    public const string FilterKey = "Hello";
    public const string IgnoreKey = "World";
}

public class ExpressionGlobalFilter : IGlobalFilter
{
    private readonly LambdaExpression _expression;

    public ExpressionGlobalFilter(LambdaExpression expression)
    {
        _expression = expression;
    }

    public LambdaExpression GetFilter(IResolverContext context)
    {
        return _expression;
    }
}

public static class GlobalFilterExtensions
{
    public static IObjectTypeDescriptor<T> GlobalFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, bool>> expression)
        where T : class
    {
        if (expression is not LambdaExpression expr)
            throw new GlobalFilterValidationException("Expression must be a lambda expression", typeof(T));
       
        // We have to wrap it. 
        var filter = new ExpressionGlobalFilter(expression);
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.FilterKey] = filter);
        return descriptor;
    }

    public static IObjectTypeDescriptor<T> GlobalFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        IGlobalFilter<T> filter)
    {
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.FilterKey] = filter);
        return descriptor;
    }

    public static IObjectTypeDescriptor<T> GlobalFilterIgnoreCondition<T>(
        this IObjectTypeDescriptor<T> descriptor,
        IIgnoreCondition ignoreCondition)
    {
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.IgnoreKey] = ignoreCondition);
        return descriptor;
    }

    public static IObjectFieldDescriptor UseGlobalFilter(this IObjectFieldDescriptor descriptor)
    {
        descriptor.Use<GlobalFilterApplicationMiddleware>();
        return descriptor;
    }

    public static IObjectTypeDescriptor<T> GlobalFilter<T, TContext>(
        this IObjectTypeDescriptor<T> descriptor,
        IValueExtractor<TContext> contextExtractor,
        Expression<Func<T, TContext, bool>> expression)
    
        where T : class
    {
        var filter = new GlobalFilterWithContext<T, TContext>(expression, contextExtractor);
        descriptor.GlobalFilter(filter);
        return descriptor;
    }
}

public interface IValueExtractor<T>
{
    T GetValue(IResolverContext context);
}

public static class ValueExtractor
{
    public static IValueExtractor<T> Create<T>(Func<IResolverContext, T> getter)
    {
        return new ValueExtractor<T>(getter);
    }        
}

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

public sealed class GlobalFilterWithContext<T, TContext> : IGlobalFilter<T>
{
    public Expression<Func<T, TContext, bool>> Predicate { get; }
    public IValueExtractor<TContext> ValueExtractor { get; }

    public GlobalFilterWithContext(
        Expression<Func<T, TContext, bool>> predicate,
        IValueExtractor<TContext> valueExtractor)
    {
        Predicate = predicate;
        ValueExtractor = valueExtractor;
    }
    
    public Expression<Func<T, bool>> GetFilterT(IResolverContext context)
    {
        var value = ValueExtractor.GetValue(context);
        var box = value.Box();
        var expression = Predicate;
        var queryParameter = expression.Parameters[0];
        var contextParameter = expression.Parameters[1];
        
        // (x, u) => x + u   -->   x + u
        var body = expression.Body;
        
        // x + u  -->   x + box.value
        var boxAccessExpression = box.MakeMemberAccess(); 
        var visitor = new ReplaceVariableExpressionVisitor(boxAccessExpression, contextParameter);
        body = visitor.Visit(body);
        
        // x + box.value  -->  x => x + box.value
        var lambda = Expression.Lambda<Func<T, bool>>(body, queryParameter);
        
        return lambda;    
    }

    public LambdaExpression GetFilter(IResolverContext context)
    {
        return GetFilterT(context);
    }
}

public static class GlobalFilterHelper
{
    public record struct Info(
        bool IsList,
        bool IsNonNull,
        Type UnwrappedRuntimeType,
        IReadOnlyDictionary<string, object?> ContextData);
    
    public static Info? GetTypeInfo(IType type)
    {
        bool isNonNull;
        {
            if (type is NonNullType nonNullType)
            {
                isNonNull = true;
                type = nonNullType.Type;
            }
            else
            {
                isNonNull = false;
            }
        }
        
        bool isList;
        {
            if (type is ListType listType)
            {
                isList = true;
                type = listType.ElementType;
            }
            else
            {
                isList = false;
            }
        }

        {
            if (type is NonNullType nonNullType)
                type = nonNullType.Type;
        }
        
        if (type is not IHasReadOnlyContextData contextDataProvider)
            return null;
        
        return new Info(isList, isNonNull, type.ToRuntimeType(), contextDataProvider.ContextData);
    }
    
    public static LambdaExpression? GetExpression(IResolverContext context, in Info info)
    {
        if (!info.ContextData.TryGetValue(GlobalFilterConstants.FilterKey, out var filter))
            return null;
        
        if (info.ContextData.TryGetValue(GlobalFilterConstants.IgnoreKey, out var ignoreCondition))
        {
            if (ignoreCondition is IIgnoreCondition condition
                && condition.ShouldIgnore(context))
            {
                return null;
            }
        } 

        if (filter is IGlobalFilter globalFilter)
            return globalFilter.GetFilter(context);
        
        throw new GlobalFilterValidationException("Expected a lambda expression", context.Selection.Type.ToRuntimeType());
    }

    private static MethodInfo GetGenericWhere(Type type)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
            {
                if (m.Name != "Where")
                    return false;
                
                var parameters = m.GetParameters();
                if (parameters.Length != 2)
                    return false;
                    
                Type[] genericArgsToFunc;
                if (type == typeof(Enumerable))
                {
                    var func = parameters[1];
                    genericArgsToFunc = func.ParameterType.GetGenericArguments();
                }
                else if (type == typeof(Queryable))
                {
                    var expr = parameters[1];
                    var func = expr.ParameterType.GetGenericArguments()[0];
                    genericArgsToFunc = func.GetGenericArguments();
                }
                else
                {
                    throw new InvalidOperationException($"Wrong type {type}");
                }
    
                return genericArgsToFunc.Length == 2;
            });
    }

    public static readonly MethodInfo EnumerableWhereWithoutIndexMethod = GetGenericWhere(typeof(Enumerable));
    public static readonly MethodInfo QueryableWhereWithoutIndexMethod = GetGenericWhere(typeof(Queryable));
    
    public static IQueryable WhereT(this IQueryable query, LambdaExpression expression, Type expectedType)
    {
        var genericWhere = QueryableWhereWithoutIndexMethod.MakeGenericMethod(expectedType);
        var methodCallExpression = Expression.Call(null, genericWhere, query.Expression, expression);
        query = query.Provider.CreateQuery(methodCallExpression);
        return query;
    }
}

public interface IIgnoreCondition
{
    bool ShouldIgnore(IResolverContext context);
}

public interface IGlobalFilter
{
    LambdaExpression GetFilter(IResolverContext context);
}

public interface IGlobalFilter<T> : IGlobalFilter
{
    Expression<Func<T, bool>> GetFilterT(IResolverContext context);
}

public sealed class GlobalFilterApplicationMiddleware
{
    private readonly FieldDelegate _next;

    public GlobalFilterApplicationMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    // private static readonly MethodInfo DelegateInvokeTMethod = typeof(Delegate)
    //     .GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)!; 
    
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context);
        var maybeInfo = GlobalFilterHelper.GetTypeInfo(context.Selection.Type);
        if (maybeInfo is null)
            return;
        
        var info = maybeInfo.Value; 
        var expression = GlobalFilterHelper.GetExpression(context, info);
        if (expression is null)
            return;

        var result = context.Result;
        if (result is null)
            return;
       
        // Handle the potential case where the result is not a list.
        if (result.GetType().IsAssignableTo(info.UnwrappedRuntimeType))
        {
            var lambda = expression.Compile();
            var passes = (bool) lambda.DynamicInvoke(result)!;
            if (passes)
                return;
            
            context.Result = null;
            return;
        }

        if (context.Result is IQueryable query)
        {
            context.Result = query.WhereT(expression, info.UnwrappedRuntimeType);
        }
        else if (context.Result is IEnumerable enumerable)
        {
            context.Result = enumerable.AsQueryable().WhereT(expression, info.UnwrappedRuntimeType);
        }
    }
}

public class GlobalFilterValidationException : Exception
{
    public Type Type { get; }
    
    public GlobalFilterValidationException(string message, Type type) : base(message)
    {
        Type = type;
    }

    public override string Message => $"{base.Message} for type {Type}.";
}

public class GlobalFilterProjectionFieldInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    private record struct ProjectionSetupArgs(
        Expression ProjectionExpression,
        GlobalFilterHelper.Info Info,
        LambdaExpression FilterExpression);

    private Expression HandleListFilter(ProjectionSetupArgs args)
    {
        // y => a.FilterExpression(y)
        var innerDelegate = args.FilterExpression;

        // x.Where(y => a.FilterExpression(y))
        var unwrappedType = args.Info.UnwrappedRuntimeType;
        var typedWhereMethod = GlobalFilterHelper.EnumerableWhereWithoutIndexMethod.MakeGenericMethod(unwrappedType);
        var methodInvocationExpression = Expression.Call(typedWhereMethod, args.ProjectionExpression, innerDelegate);

        return methodInvocationExpression;
    }

    private Expression HandleNonListFilter(ProjectionSetupArgs args)
    {
        // x --> a.FilterExpression(x) ? x : null
        var newExpression = ReplaceVariableExpressionVisitor.ReplaceParameterAndGetBody(args.FilterExpression, args.ProjectionExpression);
        var nullExpression = Expression.Constant(null, args.ProjectionExpression.Type);
        var ternary = Expression.Condition(newExpression, args.ProjectionExpression,nullExpression);
        
        return ternary; 
    }

    private void HandleProjection(QueryableProjectionContext context, ISelection selection, bool isAfter)
    {
        var info = GlobalFilterHelper.GetTypeInfo(selection.Type)!.Value;
        bool shouldHandleList = !isAfter && info.IsList;
        bool shouldHandleInstance = isAfter && !info.IsList;
        if (!shouldHandleList && !shouldHandleInstance)
            return;
        
        var filterExpression = GlobalFilterHelper.GetExpression(context.ResolverContext, info);
        if (filterExpression is null)
            return;
        
        var projectionExpression = context.PopInstance(); 
        var args = new ProjectionSetupArgs(projectionExpression, info, filterExpression);
        
        if (shouldHandleList)
            projectionExpression = HandleListFilter(args);
        else
            projectionExpression = HandleNonListFilter(args);
        
        context.PushInstance(projectionExpression);
    }
 
    public void BeforeProjection(QueryableProjectionContext context, ISelection selection) =>
        HandleProjection(context, selection, isAfter: false);

    public void AfterProjection(QueryableProjectionContext context, ISelection selection) =>
        HandleProjection(context, selection, isAfter: true);

    public bool CanHandle(ISelection selection)
    {
        var info = GlobalFilterHelper.GetTypeInfo(selection.Type);
        if (info is null)
            return false;

        var infoValue = info.Value;
        var contextData = infoValue.ContextData;
        
        bool hasFilter = contextData.ContainsKey(GlobalFilterConstants.FilterKey);
        if (!hasFilter)
            return false;

        void Throw(string message)
        {
            throw new GlobalFilterValidationException(message, selection.Type.ToRuntimeType());
        }
        
        if (infoValue is { IsList: false, IsNonNull: true })
            Throw("Cannot be applied to non-nullable non-list fields.");
        
        if (infoValue is { IsList: true, IsNonNull: false })
            Throw("For now, cannot be applied to nullable lists.");
        
        return true;
    }
}