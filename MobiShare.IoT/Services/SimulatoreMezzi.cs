using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Models;

namespace MobiShare.IoT.Services
{
    public class SimulatoreMezzi : BackgroundService
    {
        private readonly ILogger<SimulatoreMezzi> _logger;
        private readonly IMqttService _servizioMqtt;
        private readonly IMezzoRepository _repositoryMezzi;
        private readonly Random _random = new();

        public SimulatoreMezzi(ILogger<SimulatoreMezzi> logger, IMqttService servizioMqtt, IMezzoRepository repositoryMezzi)
        {
            _logger = logger;
            _servizioMqtt = servizioMqtt;
            _repositoryMezzi = repositoryMezzi;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken); // Attesa iniziale

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SimulaBatterieMezzi();
                    await SimulaMovimentiMezzi();
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore nella simulazione mezzi");
                }
            }
        }

        private async Task SimulaBatterieMezzi()
        {
            var mezziElettrici = await _repositoryMezzi.GetByTipoAsync(TipoMezzo.BiciElettrica);
            var monopattini = await _repositoryMezzi.GetByTipoAsync(TipoMezzo.Monopattino);

            var tuttiMezziElettrici = mezziElettrici.Concat(monopattini);

            foreach (var mezzo in tuttiMezziElettrici)
            {
                if (mezzo.Stato == StatoMezzo.InUso)
                {
                    // Simula consumo batteria durante l'uso
                    var consumo = _random.Next(1, 5);
                    var nuovaBatteria = Math.Max(0, (mezzo.PercentualeBatteria ?? 100) - consumo);

                    var messaggio = new MessaggioStatoMezzo
                    {
                        TipoMessaggio = MqttMessageType.AggiornamentoStatoMezzo,
                        MezzoId = mezzo.Id,
                        ParcheggioId = mezzo.ParcheggioDiPartenzaId,
                        TipoMezzo = mezzo.Tipo,
                        Stato = mezzo.Stato,
                        PercentualeBatteria = nuovaBatteria,
                        Latitudine = mezzo.Latitudine,
                        Longitudine = mezzo.Longitudine
                    };

                    var topic = $"mobishare/parcheggi/{mezzo.ParcheggioDiPartenzaId}/mezzi";
                    await PubblicaAggiornamentoMezzo(topic, messaggio);

                    if (nuovaBatteria <= 20)
                    {
                        _logger.LogWarning("Mezzo {MezzoId} ha batteria scarica: {Batteria}%", mezzo.Id, nuovaBatteria);
                        await _servizioMqtt.PubblicaNotificaSistemaAsync(
                            $"Batteria scarica per {mezzo.Id}: {nuovaBatteria}%", mezzo.Id);
                    }
                }
            }
        }

        private async Task SimulaMovimentiMezzi()
        {
            var mezziInUso = await _repositoryMezzi.GetByStatoAsync(StatoMezzo.InUso);

            foreach (var mezzo in mezziInUso)
            {
                // Simula piccoli movimenti GPS
                var variazioneLatitudine = (_random.NextDouble() - 0.5) * 0.001; // ~100m variazione
                var variazioneLongitudine = (_random.NextDouble() - 0.5) * 0.001;

                var nuovaLatitudine = mezzo.Latitudine + variazioneLatitudine;
                var nuovaLongitudine = mezzo.Longitudine + variazioneLongitudine;

                var messaggio = new MessaggioStatoMezzo
                {
                    TipoMessaggio = MqttMessageType.AggiornamentoStatoMezzo,
                    MezzoId = mezzo.Id,
                    ParcheggioId = mezzo.ParcheggioDiPartenzaId,
                    TipoMezzo = mezzo.Tipo,
                    Stato = mezzo.Stato,
                    PercentualeBatteria = mezzo.PercentualeBatteria,
                    Latitudine = nuovaLatitudine,
                    Longitudine = nuovaLongitudine
                };

                var topic = $"mobishare/parcheggi/{mezzo.ParcheggioDiPartenzaId}/mezzi";
                await PubblicaAggiornamentoMezzo(topic, messaggio);

                _logger.LogDebug("Aggiornata posizione {MezzoId}: {Lat}, {Lng}", mezzo.Id, nuovaLatitudine, nuovaLongitudine);
            }
        }

        private async Task PubblicaAggiornamentoMezzo(string topic, MessaggioStatoMezzo messaggio)
        {
            // In un'implementazione reale, useresti il client MQTT direttamente
            // Per ora simula la pubblicazione via il servizio MQTT
            await Task.CompletedTask;
        }
    }
}