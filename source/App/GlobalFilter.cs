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

public static class GlobalFilterExtensions
{
    public static IObjectTypeDescriptor<T> GlobalFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, bool>> expression)
        where T : class
    {
        if (expression is not LambdaExpression expr)
            throw new GlobalFilterValidationException("Expression must be a lambda expression");
        descriptor.Extend().OnBeforeCreate(x => x.ContextData[GlobalFilterConstants.FilterKey] = expr);
        return descriptor;
    }

    public static IObjectFieldDescriptor UseGlobalFilter(this IObjectFieldDescriptor descriptor)
    {
        descriptor.Use<GlobalFilterApplicationMiddleware>();
        return descriptor;
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
        
        throw new GlobalFilterValidationException("Expected a lambda expression");
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
    
                var func = parameters[1];
                var genericArgsToFunc = func.ParameterType.GetGenericArguments();
    
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

public class GlobalFilterApplicationMiddleware
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
    public GlobalFilterValidationException(string message) : base(message)
    {
    }
}

public class GlobalFilterProjectionFieldInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    private record struct ProjectionSetupArgs(
        QueryableProjectionContext Context,
        ISelection Selection,
        GlobalFilterHelper.Info Info,
        LambdaExpression FilterExpression);
    
    private void BeforeProjectionAction(ProjectionSetupArgs a)
    {
        var projectionExpression = a.Context.PopInstance();

        if (a.Info.IsList)
        {
            // x --> x.Where(y => a.FilterExpression(y))
            
            // y => a.FilterExpression(y)
            var innerDelegate = a.FilterExpression;
    
            // x.Where(y => a.FilterExpression(y))
            var unwrappedType = a.Info.UnwrappedRuntimeType;
            var typedWhereMethod = GlobalFilterHelper.EnumerableWhereWithoutIndexMethod.MakeGenericMethod(unwrappedType);
            var methodInvocationExpression = Expression.Call(typedWhereMethod, projectionExpression, innerDelegate);
            
            a.Context.PushInstance(methodInvocationExpression);
        }
        else
        {
            // x --> a.FilterExpression(x) ? x : null
            var newExpression = ReplaceVariableExpressionVisitor.ReplaceParameterAndGetBody(a.FilterExpression, projectionExpression);
            var nullExpression = Expression.Constant(null, projectionExpression.Type);
            var ternary = Expression.Condition(newExpression, projectionExpression,nullExpression);
            a.Context.PushInstance(ternary);
        }
        
        // Doesn't work right:
        
        // query People {
        //     test {
        //         parent
        //         {
        //             name,
        //             projects {
        //                 id,
        //                 projectName
        //             }
        //         }
        //     }
        // }
        
        // It should add to the null check.
        // !(_s1.Name.Contains("A")) ? _s1 is also wrong.
        // DbSet<Person>()
        //     .AsNoTracking()
        //     .Select(_s1 => new Person{ Parent = !(_s1.Name.Contains("A")) ? _s1 : null.Parent != null ? new Person{ 
        //             Name = !(_s1.Name.Contains("A")) ? _s1 : null.Parent.Name, 
        //             Projects = !(_s1.Name.Contains("A")) ? _s1 : null.Parent.Projects
        //                 .Where(p => p.ProjectName.Contains(" "))
        //                 .Select(p2 => new Project{ 
        //                         Id = p2.Id, 
        //                         ProjectName = p2.ProjectName 
        //                     }
        //                 )
        //                 .ToList() 
        //         }
        //         : default(Person) }
        //     )
    }

    private void AfterProjectionAction(ProjectionSetupArgs a)
    {
        if (a.Info.IsNonNull)
            return;

        if (a.Info.IsList)
        {
            // 1st case: (??)
            // x != null ? x : null   -->   x != null ? x.Where(y => a.FilterExpression(y)) : null
            
            // 2nd case: (??)
            // x   -->   same as above
        }
        else
        {
            // 1st case: (??)
            // x != null ? x : null   -->   x != null ? (a.FilterExpression(y) ? x : null) : null
            // Or 
            // x != null ? x : null   -->   (x != null && a.FilterExpression(x)) ? x : null
            
            // 2nd case: (??)
            // x   -->   same as above
        }
    }

    public GlobalFilterProjectionFieldInterceptor()
    {
        _afterProjection = ProjectionSetup(AfterProjectionAction);
        _beforeProjection = ProjectionSetup(BeforeProjectionAction);
    }

    private readonly Action<QueryableProjectionContext, ISelection> _beforeProjection;
    private readonly Action<QueryableProjectionContext, ISelection> _afterProjection;
    
    public void BeforeProjection(QueryableProjectionContext context, ISelection selection) =>
        _beforeProjection(context, selection);

    public void AfterProjection(QueryableProjectionContext context, ISelection selection) =>
        _afterProjection(context, selection);
    

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

        bool shouldSkip = contextData.ContainsKey(GlobalFilterConstants.IgnoreKey);
        if (shouldSkip)
            return false;

        if (infoValue is { IsList: false, IsNonNull: true })
            throw new GlobalFilterValidationException("Cannot be applied to non-nullable fields");
        
        return true;
    }
    
    private Action<QueryableProjectionContext, ISelection> ProjectionSetup(Action<ProjectionSetupArgs> action)
    {
        return (context, selection) =>
        {
            var info = GlobalFilterHelper.GetTypeInfo(selection.Type)!.Value;
            // NOTE: gonna make the expression twice, need to refactor the cases into a bool check.
            var filterExpression = GlobalFilterHelper.GetExpression(context.ResolverContext, info);
            if (filterExpression is null)
                return;
            
            action(new ProjectionSetupArgs
            {
                Context = context,
                Selection = selection,
                Info = info,
                FilterExpression = filterExpression,
            });
        };
    }
}