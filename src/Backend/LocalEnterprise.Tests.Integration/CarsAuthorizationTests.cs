using Microsoft.AspNetCore.Mvc.Testing;

namespace LocalEnterprise.Tests.Integration;

[Collection("Environment variables")]
public class CarsAuthorizationTests : IClassFixture<WebApplicationFactory<LocalEnterprise.Api.Security.JwtOptions>>
{
    private readonly WebApplicationFactory<LocalEnterprise.Api.Security.JwtOptions> _factory;

    public CarsAuthorizationTests(WebApplicationFactory<LocalEnterprise.Api.Security.JwtOptions> factory)
    {
        Environment.SetEnvironmentVariable("MongoDb__ConnectionString", IntegrationMongoSettings.ResolveConnectionString());
        Environment.SetEnvironmentVariable("MongoDb__DatabaseName", $"test-local-enterprise-api-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://localhost:7081");
        Environment.SetEnvironmentVariable("Jwt__Audience", "localenterprise.api");

        _factory = factory;
    }

    [Fact]
    public async Task CarsEndpoint_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cars");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
