using Microsoft.Extensions.DependencyInjection;
using LocalEnterprise.Application.Cars;
using LocalEnterprise.Application.Identity;

namespace LocalEnterprise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        return services;
    }
}
