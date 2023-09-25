using DB;
using Interfaces;
using Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class ServiceCollectionExtension
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ISqlConnector,SqlConnector>();
        services.AddTransient<IEntityRepo,EntityRepo>();
        services.AddTransient<IEntityService,EntityService>();
    }
}