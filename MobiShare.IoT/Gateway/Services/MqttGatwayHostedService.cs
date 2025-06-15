using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MobiShare.IoT.Gateway.Services
{
    /// <summary>
    /// Servizio hosted che gestisce il lifecycle del gateway MQTT
    /// </summary>
    public class MqttGatewayHostedService : BackgroundService
    {
        private readonly IMqttGatewayService _gatewayService;
        private readonly ILogger<MqttGatewayHostedService> _logger;

        public MqttGatewayHostedService(
            IMqttGatewayService gatewayService,
            ILogger<MqttGatewayHostedService> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Avvio MQTT Gateway...");

            try
            {
                var startResult = await _gatewayService.StartAsync();
                if (!startResult)
                {
                    _logger.LogError("Impossibile avviare il Gateway MQTT");
                    return;
                }

                _logger.LogInformation("MQTT Gateway avviato con successo");

                // Resta in esecuzione fino alla richiesta di stop
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken);

                    if (!_gatewayService.IsConnected)
                    {
                        _logger.LogWarning("Gateway disconnesso, tentativo di riconnessione...");
                        await _gatewayService.StartAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Shutdown richiesto per MQTT Gateway");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'esecuzione del Gateway MQTT");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Arresto MQTT Gateway...");
            await _gatewayService.StopAsync();
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("MQTT Gateway arrestato");
        }
    }
}