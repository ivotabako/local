using LocalEnterprise.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace LocalEnterprise.Tests.Unit;

public class MongoConfigurationValidationTests
{
    [Fact]
    public void ValidateMongoSettings_ThrowsHelpfulException_WhenSettingsAreMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => DependencyInjection.ValidateMongoSettings(configuration));

        Assert.Contains("MongoDb:ConnectionString", exception.Message);
        Assert.Contains("MongoDb:DatabaseName", exception.Message);
    }

    [Fact]
    public void ValidateMongoSettings_DoesNotThrow_WhenSettingsArePresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "local-enterprise-dev"
            })
            .Build();

        var exception = Record.Exception(() => DependencyInjection.ValidateMongoSettings(configuration));

        Assert.Null(exception);
    }
}
