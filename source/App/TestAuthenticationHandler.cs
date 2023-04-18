using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace efcore_transactions;

public class MyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class TestAuthenticationHandler : AuthenticationHandler<MyAuthenticationOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<MyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var auth))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, auth.ToString()) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}