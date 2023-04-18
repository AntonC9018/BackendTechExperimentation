using System.Linq.Expressions;
using System.Security.Claims;
using HotChocolate.Resolvers;
using HotChocolate.Types;

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

public class UserExtractor : IValueExtractor<ClaimsPrincipal>
{
    public static readonly UserExtractor Instance = new();
    
    public ClaimsPrincipal GetValue(IResolverContext context)
    {
        var httpContext = context.Service<IHttpContextAccessor>().HttpContext!;
        return httpContext.User;
    }
}

public static class UserFilterHelper
{
    public static GlobalFilterWithContext<T, ClaimsPrincipal> CreateGlobalUserFilter<T>(
        Expression<Func<T, ClaimsPrincipal, bool>> predicate)
    {
        return new(predicate, UserExtractor.Instance);
    }

    public static IObjectTypeDescriptor<T> GlobalUserFilter<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, ClaimsPrincipal, bool>> predicate)
    {
        var filter = CreateGlobalUserFilter(predicate);
        descriptor.GlobalFilter(filter);
        return descriptor;
    }
}