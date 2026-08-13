using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrganizationalSingularity.Api.IntegrationTests;

/// <summary>
/// Replaces the real AddMicrosoftIdentityWebApi scheme for tests -- a live Entra tenant
/// would make the suite flaky and slow. Reads the caller's identity from request headers
/// (set once on the HttpClient via WithTestIdentity) rather than static state, so the
/// handler carries no mutable fields and stays safe under whatever test parallelism xunit
/// chooses. Claims are duplicated across every claim-type spelling TenantAuthorization and
/// Microsoft.Identity.Web's ClaimsPrincipal extensions (GetObjectId/GetDisplayName/
/// GetLoginHint) might check, so the real authorization code path -- not a bypassed one --
/// is what the golden-path test actually exercises.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string OidHeader = "X-Test-Oid";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(OidHeader, out var oidValues) || string.IsNullOrEmpty(oidValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var oid = oidValues.ToString();
        var email = Request.Headers.TryGetValue(EmailHeader, out var emailValues) ? emailValues.ToString() : "";
        var name = Request.Headers.TryGetValue(NameHeader, out var nameValues) ? nameValues.ToString() : "";

        var claims = new List<Claim>
        {
            new("oid", oid),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", oid),
            new(ClaimTypes.NameIdentifier, oid),
            new("email", email),
            new(ClaimTypes.Email, email),
            new("preferred_username", email),
            new(ClaimTypes.Upn, email),
            new("name", name),
            new(ClaimTypes.Name, name),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
