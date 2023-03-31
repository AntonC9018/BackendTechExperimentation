using System.Linq.Expressions;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace efcore_transactions;

public class WhereContext
{
    public readonly HashSet<IOutputType> CanReachTargetType;
    public readonly IOutputType TargetType;
    public readonly Func<Expression, Expression> CheckExpressionFactory;

    public WhereContext(HashSet<IOutputType> canReachTargetType, IOutputType targetType, Func<Expression, Expression> checkExpressionFactory)
    {
        CanReachTargetType = canReachTargetType;
        TargetType = targetType;
        CheckExpressionFactory = checkExpressionFactory;
    }
}
public class WhereMiddleware
{
    private readonly FieldDelegate _next;
    private readonly WhereContext _where;

    public WhereMiddleware(
        FieldDelegate next,
        WhereContext where)
    {
        _ = next ?? throw new ArgumentNullException(nameof(next));
        _ = where ?? throw new ArgumentNullException(nameof(where));
        
        _next = next;
        _where = where;
    }

    // this method must be called InvokeAsync or Invoke
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context);
        
        List<Expression> expressions = new();

        var selection = context.Selection;
        foreach (var expression in AddExpressions(context, context.Selection, null))
        {
            Console.WriteLine(expression.ToString());
        }
    }

    private record struct MaybeExpression(Expression? expression, ISelection? selectionWithListKind);

    private IEnumerable<Expression> AddExpressions(
        IMiddlewareContext context,
        ISelection selection,
        Expression? memberAccessChain)
    {
        var field = selection.Field;
        bool isTargetType = field.Type == _where.TargetType;
        bool canReachTargetType = _where.CanReachTargetType.Contains(field.Type);
        
        // Avoid the the member expression allocation.
        if (!canReachTargetType && !isTargetType)
            yield break;

        memberAccessChain = memberAccessChain is null
            ? Expression.Parameter(field.RuntimeType, "x")
            : Expression.MakeMemberAccess(memberAccessChain, field.Member!);
        
        if (isTargetType)
        {
            var expression = _where.CheckExpressionFactory(memberAccessChain);
            yield return expression;
        }
        
        if (!canReachTargetType)
            yield break;

        var selectionSet = selection.SelectionSet;
        if (selectionSet is null)
            yield break;
        
        var selections = selectionSet.Selections;
        if (selections.Count == 0)
            yield break;

        foreach (var s in context.GetSelections((IObjectType) selection.Type, selection))
        {
            foreach (var expression in AddExpressions(context, s, memberAccessChain))
            {
                yield return expression;
            }
        }
    }
}