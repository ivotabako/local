using Microsoft.Extensions.DependencyInjection;

namespace LocalEnterprise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
