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

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // Configurazione MQTT Gateway
            builder.Services.Configure<MqttGatewayConfig>(
                builder.Configuration.GetSection("MqttGateway"));

            // Servizi core
            builder.Services.AddSingleton<IDeviceMappingService, DeviceMappingService>();
            builder.Services.AddHttpClient<IBackendApiService, BackendApiService>();
            builder.Services.AddSingleton<IMqttGatewayService, MqttGatewayService>();

            // Servizi hosted
            builder.Services.AddHostedService<MqttGatewayHostedService>();
            builder.Services.AddHostedService<DeviceSimulatorService>();

            // Logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            // CORS configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            app.UseCors("AllowAll");

            app.MapControllers();

            Console.WriteLine($"Gateway IoT avviato su: http://localhost:5001");

            try
            {
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetService<ILogger<Program>>();
                logger?.LogError(ex, "Errore critico nell'avvio del Gateway IoT");
                throw;
            }
        }
    }
}
