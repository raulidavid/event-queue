using Microsoft.Extensions.DependencyInjection;
using Jiban.Infrastructure.HostedServices;
using Microsoft.Extensions.Configuration;
using Jiban.BaseCode.PermissionsCode;
using Jiban.Infrastructure.Services;

namespace Jiban.Infrastructure.Configuration
{
    /// <summary>
    /// Extension methods for configuring infrastructure services and hosted services
    /// in the dependency injection container.
    /// </summary>
    public static partial class InfraestructureExtension
    {
        /// <summary>
        /// Registers Jiban hosted services with the service collection based on configuration settings.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration instance containing application settings.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddJibanHostedServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Check if background processing is enabled via configuration
            // The PROCESS configuration key determines whether to run hosted services
            if(Convert.ToBoolean(configuration[JibanConstants.PROCESS]))
            {
                // Register the electronic document processing hosted service
                // This service will run in the background to process electronic documents
                services.AddHostedService<ElectronicDocHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Registers infrastructure services with the service collection.
        /// These are the core business services used throughout the application.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration instance (currently unused but available for future use).</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register the electronic document service with scoped lifetime
            // Scoped services are created once per request in web applications
            services.AddScoped<IElectronicDocService, ElectronicDocService>();
            
            return services;
        }
    }
} 