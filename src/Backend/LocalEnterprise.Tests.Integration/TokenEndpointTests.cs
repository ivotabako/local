using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LocalEnterprise.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using OtpNet;

namespace LocalEnterprise.Tests.Integration;

[Collection("Environment variables")]
public class TokenEndpointTests : IClassFixture<WebApplicationFactory<AuthAssemblyMarker>>
{
    private const string ValidPassword = "CorrectHorseBatteryStaple_123!";

    private readonly string _databaseName;
    private readonly WebApplicationFactory<AuthAssemblyMarker> _factory;

    public TokenEndpointTests(WebApplicationFactory<AuthAssemblyMarker> factory)
    {
        _databaseName = $"test-local-enterprise-auth-{Guid.NewGuid():N}";

        Environment.SetEnvironmentVariable("MongoDb__ConnectionString", IntegrationMongoSettings.ResolveConnectionString());
        Environment.SetEnvironmentVariable("MongoDb__DatabaseName", _databaseName);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://localhost:7081");
        Environment.SetEnvironmentVariable("Jwt__Audience", "localenterprise.api");
        Environment.SetEnvironmentVariable("Auth__BootstrapAdmin__Username", "apiadmin");
        Environment.SetEnvironmentVariable("Auth__BootstrapAdmin__Password", ValidPassword);
        Environment.SetEnvironmentVariable("Auth__BootstrapAdmin__Roles__0", "Admin");

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

    [Fact]
    public async Task UsersMe_ReturnsCurrentAuthenticatedUser()
    {
        using var client = CreateSecureClient();
        var token = await AuthorizeInteractiveLoginAndGetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(payload);
        Assert.Equal("apiadmin", payload!.Username);
        Assert.Contains("Admin", payload.Roles, StringComparer.Ordinal);
    }

    [Fact]
    public async Task AdminCrud_CreatesUpdatesListsAndDeletesUsers()
    {
        using var client = CreateSecureClient();
        var token = await AuthorizeInteractiveLoginAndGetTokenAsync();

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new
            {
                username = "reader.user",
                password = "ReaderPassword_1234!",
                roles = new[] { "Reader" }
            })
        };

        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(created);

        var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserPayload>>();
        Assert.NotNull(users);
        Assert.Contains(users!, x => x.Username == "reader.user");
        Assert.Contains(users!, x => x.Username == "reader.user" && x.RequiresPasswordChange);

        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{created!.Id}")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new
            {
                roles = new[] { "Writer" }
            })
        };

        var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(updated);
        Assert.Contains("Writer", updated!.Roles, StringComparer.Ordinal);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{created.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ClearsRequiredFlag_ForCurrentUser()
    {
        using var adminClient = CreateSecureClient();
        var adminToken = await AuthorizeInteractiveLoginAndGetTokenAsync();

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
            Content = JsonContent.Create(new
            {
                username = "writer.user",
                password = "WriterPassword_1234!",
                roles = new[] { "Writer" }
            })
        };

        var createResponse = await adminClient.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        using var client = CreateSecureClient();
        var token = await AuthorizeInteractiveLoginAndGetTokenAsync("writer.user", "WriterPassword_1234!");

        var meBeforeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        meBeforeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meBefore = await client.SendAsync(meBeforeRequest);
        meBefore.EnsureSuccessStatusCode();
        var beforePayload = await meBefore.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(beforePayload);
        Assert.True(beforePayload!.RequiresPasswordChange);

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/change-password")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new
            {
                currentPassword = "WriterPassword_1234!",
                newPassword = "WriterPassword_5678!"
            })
        };

        var changeResponse = await client.SendAsync(changeRequest);
        changeResponse.EnsureSuccessStatusCode();
        var changedPayload = await changeResponse.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(changedPayload);
        Assert.False(changedPayload!.RequiresPasswordChange);

        var meAfterRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        meAfterRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meAfter = await client.SendAsync(meAfterRequest);
        meAfter.EnsureSuccessStatusCode();
        var afterPayload = await meAfter.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(afterPayload);
        Assert.False(afterPayload!.RequiresPasswordChange);
    }

    [Fact]
    public async Task LockedUser_CannotLogin_UntilUnlockedByAdmin()
    {
        using var adminClient = CreateSecureClient();
        var adminToken = await AuthorizeInteractiveLoginAndGetTokenAsync();

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
            Content = JsonContent.Create(new
            {
                username = "locked.user",
                password = "LockedPassword_1234!",
                roles = new[] { "Reader" }
            })
        };

        var created = await (await adminClient.SendAsync(createRequest)).Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(created);

        using var lockRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{created!.Id}/lock");
        lockRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var lockResponse = await adminClient.SendAsync(lockRequest);
        lockResponse.EnsureSuccessStatusCode();

        using var blockedClient = CreateSecureClient(allowAutoRedirect: false, handleCookies: true);
        var authorizePath =
            $"/connect/authorize?client_id=localenterprise-web&response_type=code&redirect_uri={Uri.EscapeDataString("https://localhost:4200/auth/callback")}&scope=localenterprise.api&state=locked-state&code_challenge=locked-verifier&code_challenge_method=plain";
        var authorizeResponse = await blockedClient.GetAsync(authorizePath);
        var returnUrl = ExtractQueryParameter(authorizeResponse.Headers.Location!, "returnUrl");

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "locked.user",
                ["password"] = "LockedPassword_1234!",
                ["returnUrl"] = returnUrl
            })
        };

        var loginResponse = await blockedClient.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Contains("error=", loginResponse.Headers.Location!.ToString(), StringComparison.Ordinal);

        using var unlockRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{created.Id}/unlock");
        unlockRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockResponse = await adminClient.SendAsync(unlockRequest);
        unlockResponse.EnsureSuccessStatusCode();

        var token = await AuthorizeInteractiveLoginAndGetTokenAsync("locked.user", "LockedPassword_1234!");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task AdminResetPassword_RequiresPasswordChange_And_TwoFactorChallengeIsEnforced()
    {
        using var adminClient = CreateSecureClient();
        var adminToken = await AuthorizeInteractiveLoginAndGetTokenAsync();

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
            Content = JsonContent.Create(new
            {
                username = "mfa.user",
                password = "StartPassword_1234!",
                roles = new[] { "Writer" }
            })
        };

        var created = await (await adminClient.SendAsync(createRequest)).Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(created);

        var resetRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{created!.Id}/reset-password")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
            Content = JsonContent.Create(new
            {
                newPassword = "ResetPassword_5678!"
            })
        };

        var resetResponse = await adminClient.SendAsync(resetRequest);
        resetResponse.EnsureSuccessStatusCode();
        var resetPayload = await resetResponse.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(resetPayload);
        Assert.True(resetPayload!.RequiresPasswordChange);

        var userToken = await AuthorizeInteractiveLoginAndGetTokenAsync("mfa.user", "ResetPassword_5678!");
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var meResponse = await adminClient.SendAsync(meRequest);
        meResponse.EnsureSuccessStatusCode();
        var mePayload = await meResponse.Content.ReadFromJsonAsync<UserPayload>();
        Assert.NotNull(mePayload);
        Assert.True(mePayload!.RequiresPasswordChange);

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/change-password")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", userToken) },
            Content = JsonContent.Create(new
            {
                currentPassword = "ResetPassword_5678!",
                newPassword = "ChangedPassword_9012!"
            })
        };

        var changeResponse = await adminClient.SendAsync(changeRequest);
        changeResponse.EnsureSuccessStatusCode();

        var enrollmentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/2fa/enrollment");
        enrollmentRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var enrollmentResponse = await adminClient.SendAsync(enrollmentRequest);
        enrollmentResponse.EnsureSuccessStatusCode();
        var enrollmentPayload = await enrollmentResponse.Content.ReadFromJsonAsync<TwoFactorEnrollmentPayload>();
        Assert.NotNull(enrollmentPayload);

        var totpCode = new Totp(Base32Encoding.ToBytes(enrollmentPayload!.SharedSecret)).ComputeTotp();

        var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/2fa/verify")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", userToken) },
            Content = JsonContent.Create(new
            {
                code = totpCode
            })
        };

        var verifyResponse = await adminClient.SendAsync(verifyRequest);
        verifyResponse.EnsureSuccessStatusCode();
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<TwoFactorVerificationPayload>();
        Assert.NotNull(verifyPayload);
        Assert.Equal(8, verifyPayload!.RecoveryCodes.Length);

        using var loginClient = CreateSecureClient(allowAutoRedirect: false, handleCookies: true);
        var authorizePath =
            $"/connect/authorize?client_id=localenterprise-web&response_type=code&redirect_uri={Uri.EscapeDataString("https://localhost:4200/auth/callback")}&scope=localenterprise.api&state=mfa-state&code_challenge=mfa-verifier&code_challenge_method=plain";
        var authorizeResponse = await loginClient.GetAsync(authorizePath);
        var returnUrl = ExtractQueryParameter(authorizeResponse.Headers.Location!, "returnUrl");

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "mfa.user",
                ["password"] = "ChangedPassword_9012!",
                ["returnUrl"] = returnUrl
            })
        };

        var loginResponse = await loginClient.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.StartsWith("/account/login-2fa", loginResponse.Headers.Location!.ToString(), StringComparison.Ordinal);

        using var twoFactorRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login-2fa")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = verifyPayload.RecoveryCodes[0],
                ["returnUrl"] = returnUrl
            })
        };

        var twoFactorResponse = await loginClient.SendAsync(twoFactorRequest);
        Assert.Equal(HttpStatusCode.Redirect, twoFactorResponse.StatusCode);
        Assert.NotNull(twoFactorResponse.Headers.Location);
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

    private async Task<string> AuthorizeInteractiveLoginAndGetTokenAsync(string username = "apiadmin", string password = ValidPassword)
    {
        const string redirectUri = "https://localhost:4200/auth/callback";
        const string state = "state-auth-users";
        const string verifier = "plain-verifier-auth-users";

        using var client = CreateSecureClient(allowAutoRedirect: false, handleCookies: true);

        var authorizePath =
            $"/connect/authorize?client_id=localenterprise-web&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=localenterprise.api&state={state}&code_challenge={verifier}&code_challenge_method=plain";

        var authorizeResponse = await client.GetAsync(authorizePath);
        var returnUrl = ExtractQueryParameter(authorizeResponse.Headers.Location!, "returnUrl");

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["returnUrl"] = returnUrl
            })
        };

        var loginResponse = await client.SendAsync(loginRequest);
        var continueAuthorizeResponse = await client.GetAsync(loginResponse.Headers.Location);
        var callbackUri = continueAuthorizeResponse.Headers.Location!;
        var authorizationCode = ExtractQueryParameter(callbackUri, "code");

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
        return payload!.access_token;
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
    private sealed record UserPayload(Guid Id, string Username, string[] Roles, DateTime CreatedAt, string? CreatedBy, bool RequiresPasswordChange, DateTime? LastPasswordChangedAt, bool IsLocked = false, bool TwoFactorEnabled = false, int RecoveryCodesRemaining = 0);
    private sealed record TwoFactorEnrollmentPayload(string SharedSecret, string ProvisioningUri, bool TwoFactorEnabled);
    private sealed record TwoFactorVerificationPayload(UserPayload User, string[] RecoveryCodes);
    private sealed record OpenIdErrorResponse(string error, string? error_description);

    private sealed record OpenIdConfiguration(
        string issuer,
        string authorization_endpoint,
        string token_endpoint,
        string[] grant_types_supported,
        string[] code_challenge_methods_supported);
}