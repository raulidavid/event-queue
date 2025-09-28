using Jiban.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Jiban.Infrastructure.Services;
using Jiban.Infrastructure.HostedServices;

namespace Jiban.Infrastructure.Configuration
{
    public static partial class InfraestructureExtension
    {
        public static IServiceCollection AddJibanHostedServices(this IServiceCollection services, IConfiguration configuration)
        {
            if(Convert.ToBoolean(configuration[BaseConstants.PROCESAR]))
            {
                services.AddHostedService<ElectronicDocHostedService>();
            }

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IElectronicDocService, ElectronicDocService>();
            return services;
        }
    }
}