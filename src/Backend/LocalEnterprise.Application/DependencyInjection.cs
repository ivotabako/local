using Microsoft.Extensions.DependencyInjection;
using LocalEnterprise.Application.Cars;

namespace LocalEnterprise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICarService, CarService>();
        return services;
    }
}
