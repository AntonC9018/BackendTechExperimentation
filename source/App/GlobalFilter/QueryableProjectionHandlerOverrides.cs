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
        var maybeContext = GlobalFilterProjectionLogic.GetContext(
            context.ResolverContext, selection.Type);
        
        // Condition(x.Field)
        Expression? checkExpression = null;
        if (maybeContext is not null)
        {
            checkExpression = GlobalFilterProjectionLogic.HandleNonListFilter(
                nestedProperty,
                maybeContext.Value.FilterExpression);
        }
        
        // x.Field != null
        Expression? nullCheckExpression = null;
        if (context.InMemory
            // Have to check for null if we'll be doing further checks (?)
            || checkExpression is not null)
        {
            nullCheckExpression = GlobalFilterHelper.NotNull(nestedProperty);
        }
       
        // x.Field != null && Condition(x.Field)
        Expression? fullCondition = null;
        if (nullCheckExpression is not null)
            fullCondition = nullCheckExpression;
        if (checkExpression is not null)
            fullCondition = Expression.AndAlso(fullCondition!, checkExpression);

        // x.Field != null && Condition(x.Field) ? Projection(x.Field) : default
        Expression memberInit = queryableScope.CreateMemberInit();
        Expression rhsExpression;
        if (fullCondition is not null)
        {
            var defaultT = Expression.Default(nestedProperty.Type);
            var ternary = Expression.Condition(
                fullCondition, memberInit, defaultT);
            rhsExpression = ternary;
        }
        else
        {
            rhsExpression = memberInit;
        }
        
        parentScope.AddLevel(field.Member, rhsExpression);
        
        action = SelectionVisitor.Continue;
        return true;
    }
}