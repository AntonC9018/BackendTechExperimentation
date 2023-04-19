using System.Linq.Expressions;

namespace efcore_transactions;

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

    public static Expression ReplaceParameterAndGetBody(LambdaExpression lambda, Expression parameterReplacement)
    {
        var visitor = new ReplaceVariableExpressionVisitor(parameterReplacement, lambda.Parameters[0]);
        return visitor.Visit(lambda.Body);
    }
}