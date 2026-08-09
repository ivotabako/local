using System.Collections.Generic;
using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Infrastructure.Configuration;
using LocalEnterprise.Infrastructure.Persistence.Cars;
using LocalEnterprise.Infrastructure.Persistence.Identity;
using LocalEnterprise.Infrastructure.Persistence;
using LocalEnterprise.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LocalEnterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ValidateMongoSettings(configuration);

        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.ConnectionString), "MongoDb:ConnectionString must be set")
            .Validate(x => !string.IsNullOrWhiteSpace(x.DatabaseName), "MongoDb:DatabaseName must be set")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddScoped<IOrderRepository, MongoOrderRepository>();
        services.AddScoped<ICarRepository, MongoCarRepository>();
        services.AddScoped<IUserAccountRepository, MongoUserAccountRepository>();
        services.AddSingleton<IPasswordHashService, AspNetPasswordHashService>();
        services.AddSingleton<IMfaService, OtpNetMfaService>();
        return services;
    }

    public static void ValidateMongoSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection(MongoOptions.SectionName);
        var missingSettings = new List<string>();

        if (string.IsNullOrWhiteSpace(section["ConnectionString"]))
        {
            missingSettings.Add("MongoDb:ConnectionString");
        }

        if (string.IsNullOrWhiteSpace(section["DatabaseName"]))
        {
            missingSettings.Add("MongoDb:DatabaseName");
        }

        if (missingSettings.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required MongoDB settings: {string.Join(", ", missingSettings)}. " +
                "Set them with dotnet user-secrets or environment variables before starting the API.");
        }
    }
}
