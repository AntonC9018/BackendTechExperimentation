using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace efcore_transactions;

public static class QueryableProjectionHelper
{
    public static void AddLevel(this QueryableProjectionScope scope, MemberInfo member, Expression rhs)
    {
        var memberBinding = Expression.Bind(member, rhs);
        scope.Level.Peek().Enqueue(memberBinding);
    }
   
    // Copy-pasted from HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder
    // because it's internal.
    private static readonly ConstantExpression _null =
        Expression.Constant(null, typeof(object));
   
    public static Expression NotNull(Expression expression)
    {
        return Expression.NotEqual(expression, _null);
    }
    
    public static Expression NotNullAndAlso(Expression property, Expression condition)
    {
        return Expression.Condition(
            NotNull(property),
            condition,
            Expression.Default(property.Type));
    }
}

// Copy-pasted from the source code, because there's no other way to implement what I want.
// Just subclassing won't cut it.
public class GlobalFilterQueryableProjectionListHandler : QueryableProjectionListHandler
{
    public override bool CanHandle(ISelection selection)
    {
        return base.CanHandle(selection)
            && GlobalFilterProjectionLogic.CanHandle(selection);
    }
    
    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;
        if (field.Member is null)
        {
            action = null;
            return false;
        }

        var scope = context.PopScope();
        if (scope is not QueryableProjectionScope queryableScope
            || !context.TryGetQueryableScope(out var parentScope))
        {
            action = null;
            return false;
        }

        // in case the projection is empty we do not project. This can happen if the
        // field handler below skips fields
        if (!queryableScope.HasAbstractTypes() &&
            (queryableScope.Level.Count == 0 || queryableScope.Level.Peek().Count == 0))
        {
            action = SelectionVisitor.Continue;
            return true;
        }
        
        Expression memberAccessExpression = context.PopInstance();
        var maybeContext = GlobalFilterProjectionLogic.GetContext(
            context.ResolverContext, selection.Type);
        Expression rhsExpression;
        if (maybeContext is null)
        {
            rhsExpression = memberAccessExpression;
        }
        else
        {
            var (runtimeType, filterExpression) = maybeContext.Value;
            rhsExpression = GlobalFilterProjectionLogic.HandleListFilter(
                memberAccessExpression,
                filterExpression,
                runtimeType);
        }
        
        var type = field.Member.GetReturnType();
        var select = queryableScope.CreateSelection(rhsExpression, type);
        parentScope.AddLevel(field.Member, select);

        action = SelectionVisitor.Continue;
        return true;
    }
}

// Copy-pasted from the source code, because there's no other way to implement what I want.
// Just subclassing won't cut it.
public class GlobalFilterQueryableProjectionFieldHandler : QueryableProjectionFieldHandler
{
    public override bool CanHandle(ISelection selection)
    {
        return base.CanHandle(selection)
            && GlobalFilterProjectionLogic.CanHandle(selection);
    }
    
    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        ISelection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (field.Member is null)
        {
            action = null;
            return false;
        }

        // Dequeue last
        var scope = context.PopScope();
        if (scope is not QueryableProjectionScope queryableScope)
        {
            action = null;
            return false;
        }

        if (!context.TryGetQueryableScope(out var parentScope))
            throw new Exception("Throwing my own exception here, because theirs is internal");

        if (field.Member is not PropertyInfo propertyInfo)
        {
            action = SelectionVisitor.Skip;
            return true;
        }
       
        Expression nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
        Expression memberInit = queryableScope.CreateMemberInit();
        var maybeContext = GlobalFilterProjectionLogic.GetContext(
            context.ResolverContext, selection.Type);
        
        // Note that we now always add the null check.
        // InMemory wasn't supposed to be true ever anyway.
        Expression rhsExpression;
        if (maybeContext is null)
        {
            rhsExpression = QueryableProjectionHelper.NotNullAndAlso(
                nestedProperty,
                memberInit);
        }
        else
        {
            rhsExpression = GlobalFilterProjectionLogic.HandleNonListFilter(
                memberInit,
                nestedProperty,
                maybeContext.Value.FilterExpression);
        }
        
        parentScope.AddLevel(field.Member, rhsExpression);
        
        action = SelectionVisitor.Continue;
        return true;
    }
}