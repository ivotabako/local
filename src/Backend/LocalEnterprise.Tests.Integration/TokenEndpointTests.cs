using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LocalEnterprise.Auth.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LocalEnterprise.Tests.Integration;

public class TokenEndpointTests : IClassFixture<WebApplicationFactory<AuthUser>>
{
    private const string ValidPassword = "CorrectHorseBatteryStaple_123!";

    private readonly WebApplicationFactory<AuthUser> _factory;

    public TokenEndpointTests(WebApplicationFactory<AuthUser> factory)
    {
        var templateUser = new AuthUser { Username = "apiadmin" };
        var passwordHasher = new PasswordHasher<AuthUser>();
        var passwordHash = passwordHasher.HashPassword(templateUser, ValidPassword);

        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://localhost:7081");
        Environment.SetEnvironmentVariable("Jwt__Audience", "localenterprise.api");
        Environment.SetEnvironmentVariable("Auth__Users__0__Username", templateUser.Username);
        Environment.SetEnvironmentVariable("Auth__Users__0__PasswordHash", passwordHash);
        Environment.SetEnvironmentVariable("Auth__Users__0__Roles__0", "Admin");

        _factory = factory;
    }

    [Fact]
    public async Task ConnectToken_RejectsPasswordGrant()
    {
        using var client = CreateSecureClient();

        using var request = CreatePasswordGrantRequest(ValidPassword);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<OpenIdErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("unsupported_grant_type", payload!.error);
    }

    [Fact]
    public async Task DiscoveryDocument_ExposesAuthorizationCodeCapabilities()
    {
        using var client = CreateSecureClient();

        var payload = await client.GetFromJsonAsync<OpenIdConfiguration>("/.well-known/openid-configuration");

        Assert.NotNull(payload);
        Assert.StartsWith("https://localhost", payload!.issuer, StringComparison.Ordinal);
        Assert.EndsWith("/connect/authorize", payload.authorization_endpoint, StringComparison.Ordinal);
        Assert.EndsWith("/connect/token", payload.token_endpoint, StringComparison.Ordinal);
        Assert.Contains("authorization_code", payload.grant_types_supported, StringComparer.Ordinal);
        Assert.Contains("S256", payload.code_challenge_methods_supported, StringComparer.Ordinal);
    }

    [Fact]
    public async Task AuthorizationCodeFlow_ReturnsAccessToken_AfterInteractiveLogin()
    {
        const string redirectUri = "https://localhost:4200/auth/callback";
        const string state = "state-abc-123";
        const string verifier = "plain-verifier-abc-123";

        using var client = CreateSecureClient(allowAutoRedirect: false, handleCookies: true);

        var authorizePath =
            $"/connect/authorize?client_id=localenterprise-web&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=localenterprise.api&state={state}&code_challenge={verifier}&code_challenge_method=plain";

        var authorizeResponse = await client.GetAsync(authorizePath);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.NotNull(authorizeResponse.Headers.Location);
        Assert.StartsWith("/account/login", authorizeResponse.Headers.Location!.ToString(), StringComparison.Ordinal);

        var loginLocation = authorizeResponse.Headers.Location;
        var loginPageResponse = await client.GetAsync(loginLocation);
        Assert.Equal(HttpStatusCode.OK, loginPageResponse.StatusCode);

        var returnUrl = ExtractQueryParameter(loginLocation!, "returnUrl");
        Assert.False(string.IsNullOrWhiteSpace(returnUrl));

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "apiadmin",
                ["password"] = ValidPassword,
                ["returnUrl"] = returnUrl
            })
        };

        var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.NotNull(loginResponse.Headers.Location);

        var continueAuthorizeResponse = await client.GetAsync(loginResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, continueAuthorizeResponse.StatusCode);
        Assert.NotNull(continueAuthorizeResponse.Headers.Location);

        var callbackUri = continueAuthorizeResponse.Headers.Location!;
        Assert.Equal(redirectUri, callbackUri.GetLeftPart(UriPartial.Path));
        Assert.Equal(state, ExtractQueryParameter(callbackUri, "state"));
        var authorizationCode = ExtractQueryParameter(callbackUri, "code");
        Assert.False(string.IsNullOrWhiteSpace(authorizationCode));

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = "localenterprise-web",
                ["code"] = authorizationCode,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = verifier
            })
        };

        var tokenResponse = await client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var payload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.access_token));
        Assert.Equal("Bearer", payload.token_type);
        Assert.True(payload.expires_in > 0);
    }

    [Fact]
    public async Task Logout_ClearsSessionCookie_AndRequiresLoginAgain()
    {
        const string redirectUri = "https://localhost:4200/auth/callback";
        const string state = "logout-state";
        const string verifier = "logout-verifier";

        using var client = CreateSecureClient(allowAutoRedirect: false, handleCookies: true);

        var authorizePath =
            $"/connect/authorize?client_id=localenterprise-web&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=localenterprise.api&state={state}&code_challenge={verifier}&code_challenge_method=plain";

        var initialAuthorizeResponse = await client.GetAsync(authorizePath);
        Assert.Equal(HttpStatusCode.Redirect, initialAuthorizeResponse.StatusCode);
        var returnUrl = ExtractQueryParameter(initialAuthorizeResponse.Headers.Location!, "returnUrl");

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "apiadmin",
                ["password"] = ValidPassword,
                ["returnUrl"] = returnUrl
            })
        };

        var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var logoutResponse = await client.GetAsync($"/account/logout?postLogoutRedirectUri={Uri.EscapeDataString("https://localhost:4200/")}");
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal("https://localhost:4200/", logoutResponse.Headers.Location?.ToString());

        var authorizeAfterLogoutResponse = await client.GetAsync(authorizePath);
        Assert.Equal(HttpStatusCode.Redirect, authorizeAfterLogoutResponse.StatusCode);
        Assert.NotNull(authorizeAfterLogoutResponse.Headers.Location);
        Assert.StartsWith("/account/login", authorizeAfterLogoutResponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    private static HttpRequestMessage CreatePasswordGrantRequest(string password)
    {
        return new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = "apiadmin",
                ["password"] = password,
                ["scope"] = "localenterprise.api"
            })
        };
    }

    private HttpClient CreateSecureClient(bool allowAutoRedirect = true, bool handleCookies = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect,
            HandleCookies = handleCookies
        });
    }

    private static string ExtractQueryParameter(Uri uri, string key)
    {
        var query = uri.IsAbsoluteUri
            ? uri.Query
            : ExtractRelativeQuery(uri.OriginalString);

        var parameters = ParseQueryString(query);
        return parameters.TryGetValue(key, out var value)
            ? value
            : string.Empty;
    }

    private static string ExtractRelativeQuery(string uriValue)
    {
        var queryStart = uriValue.IndexOf('?', StringComparison.Ordinal);
        return queryStart >= 0
            ? uriValue[queryStart..]
            : string.Empty;
    }

    private static IDictionary<string, string> ParseQueryString(string query)
    {
        var trimmedQuery = query.StartsWith("?", StringComparison.Ordinal) ? query[1..] : query;
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var segment in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                result[Uri.UnescapeDataString(segment)] = string.Empty;
                continue;
            }

            var name = Uri.UnescapeDataString(segment[..separatorIndex]);
            var value = Uri.UnescapeDataString(segment[(separatorIndex + 1)..]);
            result[name] = value;
        }

        return result;
    }

    private sealed record TokenResponse(string access_token, string token_type, int expires_in);
    private sealed record OpenIdErrorResponse(string error, string? error_description);

    private sealed record OpenIdConfiguration(
        string issuer,
        string authorization_endpoint,
        string token_endpoint,
        string[] grant_types_supported,
        string[] code_challenge_methods_supported);
}