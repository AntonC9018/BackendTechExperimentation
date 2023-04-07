using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace efcore_transactions;

public static class RelationProjectionsObjectDescriptorExtensions
{
    public static IObjectFieldDescriptor Relation<T>(
        this IObjectFieldDescriptor descriptor,
        Expression<Func<T, object>> expression)
    {
        descriptor.Extend().OnBeforeCreate(x => x.ContextData["RelationProjection"] = expression);
        return descriptor;
    }

}

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
}

public class GlobalFilterValidationException : Exception
{
    public GlobalFilterValidationException(string message) : base(message)
    {
    }
}

public class HelloWorldProjectionFieldInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    private static readonly MethodInfo WhereWithoutIndexMethod = typeof(Enumerable)
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
    
    private record struct Info(bool IsList, bool IsNonNull, Type UnwrappedRuntimeType, IReadOnlyDictionary<string, object?> ContextData);
    private record struct ProjectionSetupArgs(
        QueryableProjectionContext Context,
        ISelection Selection,
        Info Info,
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
            var typedWhereMethod = WhereWithoutIndexMethod.MakeGenericMethod(unwrappedType);
            var methodInvocationExpression = Expression.Call(typedWhereMethod, projectionExpression, innerDelegate);
            
            a.Context.PushInstance(methodInvocationExpression);
        }
        else
        {
            // x --> a.FilterExpression(x) ? x : null
            var newExpression = ReplaceVariableExpressionVisitor.ReplaceAndGetBody(a.FilterExpression, projectionExpression);
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

    public HelloWorldProjectionFieldInterceptor()
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
    
    private static Info? GetInfo(ISelection selection)
    {
        var type = selection.Type;

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
    
    public bool CanHandle(ISelection selection)
    {
        var info = GetInfo(selection);
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
            var info = GetInfo(selection)!.Value;
            var filterExpression = (LambdaExpression) info.ContextData[GlobalFilterConstants.FilterKey]!;
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

public class RelationProjectionFieldInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(ISelection selection)
    {
        var field = selection.Field;
        var contextData = field.ContextData;
        return contextData.ContainsKey("RelationProjection");
    }

    public void BeforeProjection(
        QueryableProjectionContext context,
        ISelection selection)
    {
        var field = selection.Field;
        var contextData = field.ContextData;
        var filterExpression = (LambdaExpression) contextData["RelationProjection"]!;
        var fieldDefinition = field.ResolverMember;
        var instance = context.PopInstance();

        var nextInstance = ReplaceVariableExpressionVisitor.ReplaceAndGetBody(filterExpression, instance);
        
        context.PushInstance(nextInstance);
    }

    public void AfterProjection(QueryableProjectionContext context, ISelection selection)
    {
    }
}

public sealed class ReplaceVariableExpressionVisitor : ExpressionVisitor
{
    public Expression Replacement { get; set; }
    public ParameterExpression Parameter { get; set; }

    public ReplaceVariableExpressionVisitor(
        Expression replacement,
        ParameterExpression parameter)
    {
        Replacement = replacement;
        Parameter = parameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == Parameter)
            return Replacement;

        return base.VisitParameter(node);
    }

    public static Expression ReplaceAndGetBody(LambdaExpression lambda, Expression parameterReplacement)
    {
        var visitor = new ReplaceVariableExpressionVisitor(parameterReplacement, lambda.Parameters[0]);
        return visitor.Visit(lambda.Body);
    }
}