using Microsoft.Extensions.Configuration;

namespace LocalEnterprise.Tests.Integration;

internal static class IntegrationMongoSettings
{
    public static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("MongoDb__ConnectionString");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<LocalEnterprise.Api.Security.JwtOptions>()
            .Build();

        var fromSecrets = configuration["MongoDb:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromSecrets))
        {
            return fromSecrets;
        }

        throw new InvalidOperationException(
            "MongoDb connection string is not configured for integration tests. Set MongoDb__ConnectionString or add the API user secret.");
    }
}