using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

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

public class RelationProjectionInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
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

        var nextInstance = ReplaceVariableExpressionVisitor
            .ReplaceParameter(filterExpression, filterExpression.Parameters[0], instance);
        
        context.PushInstance(nextInstance);
    }

    public void AfterProjection(QueryableProjectionContext context, ISelection selection)
    {
    }
    
    private sealed class ReplaceVariableExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _replacement;
        private readonly ParameterExpression _parameter;

        public ReplaceVariableExpressionVisitor(
            Expression replacement,
            ParameterExpression parameter)
        {
            _replacement = replacement;
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _parameter)
                return _replacement;

            return base.VisitParameter(node);
        }

        public static Expression ReplaceParameter(
            LambdaExpression lambda,
            ParameterExpression parameter,
            Expression replacement)
        {
            var visitor = new ReplaceVariableExpressionVisitor(replacement, parameter);
            return visitor.Visit(lambda.Body);
        }
    }
}