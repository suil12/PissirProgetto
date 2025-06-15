using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MobiShare.IoT.Gateway.Services;
using MobiShare.IoT.Gateway.Models;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Services;

namespace MobiShare.IoT.Gateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    MobiShare IoT Gateway");
            Console.WriteLine("========================================");

            var host = CreateHostBuilder(args).Build();

            try
            {
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogError(ex, "Errore critico nell'avvio del Gateway IoT");
                throw;
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // Configurazione MQTT Gateway
                    services.Configure<MqttGatewayConfig>(
                        configuration.GetSection("MqttGateway"));

                    // Servizi core
                    services.AddSingleton<IDeviceMappingService, DeviceMappingService>();
                    services.AddHttpClient<IBackendApiService, BackendApiService>();
                    services.AddSingleton<IMqttGatewayService, MqttGatewayService>();

                    // Servizi hosted
                    services.AddHostedService<MqttGatewayHostedService>();
                    services.AddHostedService<DeviceSimulatorService>();

                    // Logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}
