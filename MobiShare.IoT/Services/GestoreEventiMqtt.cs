using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Interfaces;

namespace MobiShare.IoT.Services
{
    public class GestoreEventiMqtt : BackgroundService
    {
        private readonly ILogger<GestoreEventiMqtt> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IMqttService? _servizioMqtt;
        private GestoreEventiIoT? _gestoreEventi;

        public GestoreEventiMqtt(ILogger<GestoreEventiMqtt> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Attendi che i servizi siano disponibili
            await Task.Delay(2000, stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            _servizioMqtt = scope.ServiceProvider.GetRequiredService<IMqttService>();
            _gestoreEventi = scope.ServiceProvider.GetRequiredService<GestoreEventiIoT>();

            // Sottoscrivi agli eventi MQTT
            _servizioMqtt.StatoMezzoAggiornato += AlAggiornamentoStatoMezzo;
            _servizioMqtt.SensoreSlotAggiornato += AlAggiornamentoSensoreSlot;

            _logger.LogInformation("Gestore Eventi MQTT avviato con nomenclatura italiana");

            // Mantieni il servizio attivo
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Gestore Eventi MQTT fermato");
            }
        }

        private async void AlAggiornamentoStatoMezzo(object? sender, AggiornamentoStatoMezzoEventArgs e)
        {
            if (_gestoreEventi != null)
            {
                await _gestoreEventi.GestisciAggiornamentoStatoMezzo(e);
            }
        }

        private async void AlAggiornamentoSensoreSlot(object? sender, AggiornamentoSensoreSlotEventArgs e)
        {
            if (_gestoreEventi != null)
            {
                await _gestoreEventi.GestisciAggiornamentoSensoreSlot(e);
            }
        }

        public override void Dispose()
        {
            if (_servizioMqtt != null)
            {
                _servizioMqtt.StatoMezzoAggiornato -= AlAggiornamentoStatoMezzo;
                _servizioMqtt.SensoreSlotAggiornato -= AlAggiornamentoSensoreSlot;
            }
            base.Dispose();
        }
    }
}