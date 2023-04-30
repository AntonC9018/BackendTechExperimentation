using System.Security.Claims;
using HotChocolate.GlobalFilters;
using HotChocolate.Resolvers;

namespace efcore_transactions;

public class IsUserAdminIgnoreCondition : IIgnoreCondition
{
    public static readonly IsUserAdminIgnoreCondition Instance = new();
    public bool ShouldIgnore(IResolverContext context)
    {
        var httpContext = context.Service<IHttpContextAccessor>().HttpContext!;
        var userIsAdmin = httpContext.User.Claims.Any(c => c is { Type: "Admin", Value: "1" });
        return userIsAdmin;
    }
}

public class UserNameExtractor : IValueExtractor<string>
{
    public static readonly UserNameExtractor Instance = new();
    public string GetValue(IResolverContext context)
    {
        var httpContext = context.Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var userName = httpContext.User.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        return userName;
    }
}

public class UserIdExtractor : IValueExtractor<long?>
{
    public static readonly UserIdExtractor Instance = new();
    public long? GetValue(IResolverContext context)
    {
        var httpContext = context.Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var userName = httpContext.User.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        if (long.TryParse(userName, out long result))
            return result;
        return null;
    }
}
