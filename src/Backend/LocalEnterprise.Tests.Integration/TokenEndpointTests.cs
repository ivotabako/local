using System.Net;
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
    public async Task ConnectToken_ReturnsToken_WhenPasswordIsValid()
    {
        using var client = CreateSecureClient();

        using var request = CreatePasswordGrantRequest(ValidPassword);
        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.access_token));
        Assert.Equal("Bearer", payload.token_type);
        Assert.True(payload.expires_in > 0);
    }

    [Fact]
    public async Task ConnectToken_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        using var client = CreateSecureClient();

        using var request = CreatePasswordGrantRequest("wrong-password");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DiscoveryDocument_ExposesTokenEndpoint()
    {
        using var client = CreateSecureClient();

        var payload = await client.GetFromJsonAsync<OpenIdConfiguration>("/.well-known/openid-configuration");

        Assert.NotNull(payload);
        Assert.StartsWith("https://localhost", payload!.issuer, StringComparison.Ordinal);
        Assert.EndsWith("/connect/token", payload.token_endpoint, StringComparison.Ordinal);
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

    private HttpClient CreateSecureClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private sealed record TokenResponse(string access_token, string token_type, int expires_in);

    private sealed record OpenIdConfiguration(string issuer, string token_endpoint);
}