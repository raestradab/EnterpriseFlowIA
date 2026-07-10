using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Features.Identity.GetMyPermissions;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// End-to-end proof for Sprint 7a (Identidad): the real HTTP round trip — register a tenant,
/// log in with the credentials just created, and use the resulting token against a protected
/// endpoint — plus refresh rotation with reuse detection (HU-002, see the sequence diagram in
/// docs/03-diseno-arquitectura/04-secuencias.md).
///
/// Every test here uses a "raw" client (cookie auto-handling switched off) and reads/sends the
/// refresh token cookie explicitly (security review finding: it moved from the JSON body /
/// browser localStorage to an HttpOnly cookie). Explicit handling throughout — rather than
/// relying on the client's own cookie jar for some tests and not others — is what makes it
/// possible to simulate an attacker replaying a stale, already-rotated cookie value, which is
/// exactly the scenario the reuse-detection tests need to prove.
/// </summary>
public sealed class IdentityEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string RefreshCookieName = "refreshToken";

    private static (string Slug, string Email) UniqueTenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return ($"acme-{suffix}", $"admin-{suffix}@acme.test");
    }

    private static async Task<RegisterTenantResult> RegisterTenantAsync(HttpClient client, string slug, string email, string password = "SuperSecret123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register-tenant", new
        {
            tenantName = "Acme Corp",
            tenantSlug = slug,
            adminEmail = email,
            adminPassword = password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RegisterTenantResult>())!;
    }

    private static string ExtractRefreshCookieValue(HttpResponseMessage response)
    {
        var setCookieHeader = response.Headers.GetValues("Set-Cookie")
            .Single(h => h.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));

        var nameValuePart = setCookieHeader.Split(';')[0];
        return nameValuePart[$"{RefreshCookieName}=".Length..];
    }

    private static Task<HttpResponseMessage> RefreshWithCookieAsync(HttpClient client, string cookieValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={cookieValue}");
        return client.SendAsync(request);
    }

    /// <summary>Cookie handling switched off: this suite manages the refresh cookie by hand.</summary>
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task RegisterTenant_Then_Login_Grants_A_Working_Token()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        var registered = await RegisterTenantAsync(client, slug, email);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!;
        login.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Security review finding: the refresh token must arrive as an HttpOnly cookie, not in
        // the JSON body — it's a 30-day credential that JavaScript (and therefore any XSS
        // payload) must never be able to read.
        var setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie")
            .Single(h => h.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        setCookieHeader.Should().Contain("httponly", "the refresh token cookie must not be readable from JavaScript");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = (await meResponse.Content.ReadFromJsonAsync<MyPermissionsDto>())!;
        me.Permissions.Should().Contain("companies.manage");

        // Regression guard: ASP.NET Core's JWT handler remaps "sub" to the legacy
        // ClaimTypes.NameIdentifier URI unless MapInboundClaims is disabled — a bug that
        // slipped past this exact test until a manual smoke test caught UserId coming back
        // as Guid.Empty despite a correctly issued token (see Program.cs). Asserting only
        // Permissions (a custom claim type, unaffected by the remapping) wasn't enough.
        me.UserId.Should().Be(registered.AdminUserId);
        me.TenantId.Should().Be(registered.TenantId);
    }

    [Fact]
    public async Task RegisterTenant_With_Duplicate_Slug_Returns_BadRequest()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var (_, secondEmail) = UniqueTenant();
        var response = await client.PostAsJsonAsync("/api/auth/register-tenant", new
        {
            tenantName = "Acme Corp Again",
            tenantSlug = slug,
            adminEmail = secondEmail,
            adminPassword = "SuperSecret123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>HU-001/ADR-0006 timing-attack mitigation: <c>LoginCommandHandler</c> hashes a
    /// dummy password even when the email doesn't exist at all, specifically so a nonexistent
    /// account doesn't return faster than a wrong-password one — this is the only test that
    /// takes that branch (Sprint 9, Release 2: found 0% covered by the existing suite, which
    /// only ever exercised the "user exists, wrong password" branch above).</summary>
    [Fact]
    public async Task Login_With_Unknown_Email_Returns_Unauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = $"nobody-{Guid.NewGuid():N}@nowhere.test", password = "SuperSecret123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_Can_Create_A_Company_After_Login()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var login = (await (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" }))
            .Content.ReadFromJsonAsync<AccessTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/companies", new { name = "Created By Admin", taxId = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>A refresh token that was never issued at all (garbage string, not a
    /// stolen/rotated one) — the simplest possible attack, and one none of the reuse-detection
    /// tests below actually exercises (they all start from a real, issued token). Sprint 9,
    /// Release 2: found 0% covered.</summary>
    [Fact]
    public async Task Refresh_With_A_Token_That_Was_Never_Issued_Returns_Unauthorized()
    {
        var client = CreateClient();

        var response = await RefreshWithCookieAsync(client, "this-was-never-a-real-refresh-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Rotates_The_Token_And_Rejects_Reuse_Of_The_Old_One()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        var originalCookie = ExtractRefreshCookieValue(loginResponse);

        var firstRefreshResponse = await RefreshWithCookieAsync(client, originalCookie);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedCookie = ExtractRefreshCookieValue(firstRefreshResponse);
        rotatedCookie.Should().NotBe(originalCookie);

        // Reusing the original (now-rotated) refresh token must fail — HU-002 reuse detection.
        var reuseResponse = await RefreshWithCookieAsync(client, originalCookie);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Security review finding: reuse must revoke the *whole* chain, not just the reused
        // token — otherwise an attacker who stole the original and already rotated it once
        // keeps a live session even after the theft is detected. The rotated child must be
        // dead too (this used to assert HttpStatusCode.OK, encoding the vulnerable behavior).
        var secondRefreshResponse = await RefreshWithCookieAsync(client, rotatedCookie);
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Reuse_Revokes_The_Entire_Chain_Even_Several_Hops_Later()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        var originalCookie = ExtractRefreshCookieValue(loginResponse);

        // Simulates an attacker stealing the original token and rotating it twice before the
        // legitimate user's own (now-stale) copy of the original gets used again.
        var hop1Response = await RefreshWithCookieAsync(client, originalCookie);
        var hop1Cookie = ExtractRefreshCookieValue(hop1Response);
        var hop2Response = await RefreshWithCookieAsync(client, hop1Cookie);
        var hop2Cookie = ExtractRefreshCookieValue(hop2Response);

        // The legitimate user's stale copy of the very first token surfaces and gets reused.
        var reuseResponse = await RefreshWithCookieAsync(client, originalCookie);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The attacker's entire chain must be dead, not just the immediate child — proves the
        // chain walk doesn't stop after one hop.
        (await RefreshWithCookieAsync(client, hop1Cookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshWithCookieAsync(client, hop2Cookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Revokes_The_Refresh_Token()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        var cookie = ExtractRefreshCookieValue(loginResponse);

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"{RefreshCookieName}={cookie}");
        (await client.SendAsync(logoutRequest)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Security review finding companion: moving the token into an HttpOnly cookie only
        // helps if logout actually revokes it server-side — otherwise it just keeps working
        // for anyone holding the cookie value until its 30-day expiry regardless of "logout".
        (await RefreshWithCookieAsync(client, cookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterTenant_With_Duplicate_Email_Returns_BadRequest_Without_Confirming_It_Exists()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);

        var (secondSlug, _) = UniqueTenant();
        var response = await client.PostAsJsonAsync("/api/auth/register-tenant", new
        {
            tenantName = "Acme Corp Again",
            tenantSlug = secondSlug,
            adminEmail = email,
            adminPassword = "SuperSecret123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Security review finding: the response must not confirm the email already exists
        // (RegistrationFailedException) — this keeps the enumeration fix from regressing.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("already registered", "the error must not confirm which email exists");
    }

    [Fact]
    public async Task CreateRole_Then_AssignToUser_Succeeds()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        var registered = await RegisterTenantAsync(client, slug, email);
        await AuthenticateAsAdminAsync(client, email);

        string[] permissionsToGrant = ["projects.read"];
        var createRoleResponse = await client.PostAsJsonAsync("/api/auth/roles", new { name = "Auditor", permissionsToGrant });
        createRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = (await createRoleResponse.Content.ReadFromJsonAsync<IdResponse>())!;

        var assignResponse = await client.PostAsync($"/api/auth/users/{registered.AdminUserId}/roles/{role.Id}", content: null);

        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRoleToUser_With_Unknown_Role_Returns_NotFound()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        var registered = await RegisterTenantAsync(client, slug, email);
        await AuthenticateAsAdminAsync(client, email);

        var response = await client.PostAsync($"/api/auth/users/{registered.AdminUserId}/roles/{Guid.NewGuid()}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRoleToUser_With_Unknown_User_Returns_NotFound()
    {
        var client = CreateClient();
        var (slug, email) = UniqueTenant();
        await RegisterTenantAsync(client, slug, email);
        await AuthenticateAsAdminAsync(client, email);

        var createRoleResponse = await client.PostAsJsonAsync("/api/auth/roles", new
        {
            name = "Auditor",
            permissionsToGrant = Array.Empty<string>(),
        });
        var role = (await createRoleResponse.Content.ReadFromJsonAsync<IdResponse>())!;

        var response = await client.PostAsync($"/api/auth/users/{Guid.NewGuid()}/roles/{role.Id}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client, string email)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });
        var login = (await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private sealed record AccessTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc);

    private sealed record IdResponse(Guid Id);
}
