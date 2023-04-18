using System.Linq.Expressions;
using System.Security.Claims;
using HotChocolate.Resolvers;

namespace efcore_transactions;

public class IsUserAdminIgnoreCondition : IIgnoreCondition
{
    public bool ShouldIgnore(IResolverContext context)
    {
        var httpContext = context.Service<IHttpContextAccessor>().HttpContext!;
        var userIsAdmin = httpContext.User.Claims.Any(c => c is { Type: "Admin", Value: "1" });
        return userIsAdmin;
    }
}

public abstract class GlobalUserFilter<T> : IGlobalFilter<T>
{
    public abstract Expression<Func<T, bool>> GetFilter(ClaimsPrincipal user);

    public Expression<Func<T, bool>> GetFilterT(IResolverContext context)
    {
        var httpContext = context.Service<IHttpContextAccessor>().HttpContext!;
        return GetFilter(httpContext.User!);
    }

    public LambdaExpression GetFilter(IResolverContext context) => GetFilterT(context);
}

public class PredicateGlobalUserFilter<T> : GlobalUserFilter<T>
{
    private readonly Expression<Func<T, ClaimsPrincipal, bool>> _predicate;

    public PredicateGlobalUserFilter(Expression<Func<T, ClaimsPrincipal, bool>> predicate)
    {
        _predicate = predicate;
    }

    public sealed override Expression<Func<T, bool>> GetFilter(ClaimsPrincipal user)
    {
        // x => _predicate(x, user), where user is captured.
        var box = user.Box();
        var expression = _predicate;
        var queryParameter = expression.Parameters[0];
        var userParameter = expression.Parameters[1];
        
        // (x, u) => x + u   -->   x + u
        var body = expression.Body;
        
        // x + u  -->   x + box.value
        var boxAccessExpression = box.MakeMemberAccess(); 
        var visitor = new ReplaceVariableExpressionVisitor(boxAccessExpression, userParameter);
        body = visitor.Visit(body);
        
        // x + box.value  -->  x => x + box.value
        var lambda = Expression.Lambda<Func<T, bool>>(body, queryParameter);
        
        return lambda;
    }
}