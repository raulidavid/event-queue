using Jiban.BaseCode.PermissionsCode;
using Jiban.Infrastructure.HostedServices;
using Jiban.Infrastructure.Nswag;
using Jiban.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Jiban.Infrastructure.Configuration
{
    public static partial class InfraestructureExtension
    {
        public static IServiceCollection AddJibanHostedServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (Convert.ToBoolean(configuration[JibanConstants.PROCESS]))
            {
                services.AddHostedService<ElectronicDocHostedService>();
            }

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IElectronicDocService, ElectronicDocService>();
            services.AddScoped<ISriClient, SriClient>();
            services.AddSingleton<ITokenAccessor, RuntimeTokenAccessor>();
            services.AddTransient<DynamicJwtHandler>();
            services.AddHttpClient<NswagConfiguration>()
            .AddHttpMessageHandler<DynamicJwtHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => (int)msg.StatusCode == 429)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)), // 2^(1+1)=4, 2^(2+1)=8, 2^(3+1)=16
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"[Polly] Retry {retryCount} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                    });
        }


        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(180));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(10);
        }
    }
}