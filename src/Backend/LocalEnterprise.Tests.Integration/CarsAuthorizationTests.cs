using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LocalEnterprise.Tests.Integration;

public class CarsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CarsAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                    ["MongoDb:DatabaseName"] = "test-local-enterprise",
                    ["Jwt:Issuer"] = "https://localhost:7081",
                    ["Jwt:Audience"] = "localenterprise.api",
                    ["Jwt:SigningKey"] = "dev-localenterprise-signing-key-32chars"
                });
            });
        });
    }

    [Fact]
    public async Task CarsEndpoint_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cars");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
