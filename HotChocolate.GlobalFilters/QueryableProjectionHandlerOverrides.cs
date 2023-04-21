﻿using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

namespace HotChocolate.GlobalFilters;

public static class ProjectionHelper
{
    public static void AddLevelItem(this QueryableProjectionScope scope, MemberInfo member, Expression rhs)
    {
        var memberBinding = Expression.Bind(member, rhs);
        scope.Level.Peek().Enqueue(memberBinding);
    }

    // Copy-pasted from HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder
    // because it's internal.
    internal static readonly ConstantExpression _null =
        Expression.Constant(null, typeof(object));

    public static Expression NotNull(Expression expression)
    {
        return Expression.NotEqual(expression, _null);
    }
}

public class GlobalFilterQueryableProjectionListHandler : QueryableProjectionListHandler
{
    public override bool CanHandle(ISelection selection)
    {
        return base.CanHandle(selection)
            && GlobalFilterProjectionLogic.CanHandle(selection);
    }

    public override QueryableProjectionContext OnBeforeEnter(
        QueryableProjectionContext context,
        ISelection selection)
    {
        // This pushes the list member access onto the stack.
        context = base.OnBeforeEnter(context, selection);

        var maybeContext = GlobalFilterProjectionLogic.GetContext(
            context.ResolverContext, selection.Type);
        if (maybeContext is null)
            return context;

        var (type, filterExpression) = maybeContext.Value;
        var instance = context.PopInstance();
        instance = GlobalFilterProjectionLogic.HandleListFilter(
            instance, filterExpression, type);
        context.PushInstance(instance);
        return context;
    }
}

// Copy-pasted from the source code, because there's no other way to implement what I want.
// Just subclassing won't cut it, while the interceptors don't happen when I need them to.
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
            throw new Exception("Throwing my own exception here, because theirs is internal.");

        if (field.Member is not PropertyInfo propertyInfo)
        {
            action = SelectionVisitor.Skip;
            return true;
        }

        Expression nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);

        // This piece right here is basically why interceptors don't cut it.
        // If you could change a level item after it's been pushed, it wouldn't have been necessary.
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
            nullCheckExpression = ProjectionHelper.NotNull(nestedProperty);
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

        parentScope.AddLevelItem(field.Member, rhsExpression);

        action = SelectionVisitor.Continue;
        return true;
    }
}
