using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Models;

namespace MobiShare.IoT.Services
{
    public class ServizioMqtt : IMqttService
    {
        private readonly ILogger<ServizioMqtt> _logger;
        private readonly MqttConfig _config;
        private IManagedMqttClient? _clientMqtt;

        public event EventHandler<AggiornamentoStatoMezzoEventArgs>? StatoMezzoAggiornato;
        public event EventHandler<AggiornamentoSensoreSlotEventArgs>? SensoreSlotAggiornato;

        public ServizioMqtt(ILogger<ServizioMqtt> logger, IOptions<MqttConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task AvviaAsync()
        {
            var mqttFactory = new MqttFactory();
            _clientMqtt = mqttFactory.CreateManagedMqttClient();

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId(_config.ClientId)
                    .WithTcpServer(_config.Host, _config.Port)
                    .WithCredentials(_config.Username, _config.Password)
                    .WithCleanSession()
                    .Build())
                .Build();

            _clientMqtt.ApplicationMessageReceivedAsync += GestisciMessaggioRicevuto;
            _clientMqtt.ConnectedAsync += GestisciConnessione;
            _clientMqtt.DisconnectedAsync += GestisciDisconnessione;

            await _clientMqtt.StartAsync(options);
        }

        public async Task FermaAsync()
        {
            if (_clientMqtt != null)
            {
                await _clientMqtt.StopAsync();
                _clientMqtt.Dispose();
                _clientMqtt = null;
            }
        }

        public async Task PubblicaComandoMezzoAsync(string mezzoId, string comando, object? dati = null)
        {
            var parcheggio = EstraiParcheggioIdDaMezzoId(mezzoId);
            var topic = $"mobishare/parcheggi/{parcheggio}/stato/{mezzoId}";

            var messaggio = new MessaggioComandoMezzo
            {
                TipoMessaggio = comando == "SBLOCCA" ? MqttMessageType.ComandoSbloccoMezzo : MqttMessageType.ComandoBloccoMezzo,
                MezzoId = mezzoId,
                Comando = comando,
                Dati = dati
            };

            await PubblicaAsync(topic, messaggio);
            _logger.LogInformation("Comando {Comando} inviato al mezzo {MezzoId}", comando, mezzoId);
        }

        public async Task PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore)
        {
            var topic = $"mobishare/parcheggi/{parcheggioId}/slots/{slotId}";

            var messaggio = new MessaggioSensoreSlot
            {
                TipoMessaggio = MqttMessageType.AggiornamentoSensoreSlot,
                SlotId = slotId,
                ParcheggioId = parcheggioId,
                ColoreLuce = colore,
                StatoSlot = colore == ColoreLuce.Verde ? StatoSlot.Libero : StatoSlot.Occupato
            };

            await PubblicaAsync(topic, messaggio);
            _logger.LogInformation("Aggiornamento LED slot {SlotId} colore {Colore}", slotId, colore);
        }

        public async Task PubblicaNotificaSistemaAsync(string messaggio, string? mezzoId = null)
        {
            var topic = "mobishare/sistema/notifiche";

            var notifica = new MessaggioMqtt
            {
                TipoMessaggio = MqttMessageType.NotificaSistema,
                MezzoId = mezzoId,
                Dati = new { Messaggio = messaggio }
            };

            await PubblicaAsync(topic, notifica);
            _logger.LogInformation("Notifica sistema: {Messaggio}", messaggio);
        }

        public async Task SottoscriviAggiornamentoMezziAsync()
        {
            var topic = "mobishare/parcheggi/+/mezzi";
            await SottoscriviAsync(topic);
            _logger.LogInformation("Sottoscritto a aggiornamenti mezzi: {Topic}", topic);
        }

        public async Task SottoscriviSensoriSlotAsync()
        {
            var topic = "mobishare/parcheggi/+/slots/+";
            await SottoscriviAsync(topic);
            _logger.LogInformation("Sottoscritto a sensori slot: {Topic}", topic);
        }

        private async Task PubblicaAsync(string topic, object messaggio)
        {
            if (_clientMqtt == null || !_clientMqtt.IsConnected)
            {
                _logger.LogWarning("Client MQTT non connesso, impossibile pubblicare su {Topic}", topic);
                return;
            }

            var payload = JsonConvert.SerializeObject(messaggio);
            var messaggioMqtt = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _clientMqtt.EnqueueAsync(messaggioMqtt);
        }

        private async Task SottoscriviAsync(string topic)
        {
            if (_clientMqtt == null)
                return;

            await _clientMqtt.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
        }

        private Task GestisciMessaggioRicevuto(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var topic = args.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

                _logger.LogDebug("Messaggio ricevuto su {Topic}: {Payload}", topic, payload);

                if (topic.Contains("/mezzi"))
                {
                    GestisciAggiornamentoStatoMezzo(payload);
                }
                else if (topic.Contains("/slots/"))
                {
                    GestisciAggiornamentoSensoreSlot(payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore processamento messaggio MQTT");
            }

            return Task.CompletedTask;
        }

        private void GestisciAggiornamentoStatoMezzo(string payload)
        {
            try
            {
                var messaggio = JsonConvert.DeserializeObject<MessaggioStatoMezzo>(payload);
                if (messaggio?.MezzoId != null)
                {
                    var eventArgs = new AggiornamentoStatoMezzoEventArgs
                    {
                        MezzoId = messaggio.MezzoId,
                        ParcheggioId = messaggio.ParcheggioId ?? "",
                        Stato = messaggio.Stato,
                        PercentualeBatteria = messaggio.PercentualeBatteria,
                        Latitudine = messaggio.Latitudine,
                        Longitudine = messaggio.Longitudine,
                        Timestamp = messaggio.Timestamp
                    };

                    StatoMezzoAggiornato?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore processamento aggiornamento mezzo");
            }
        }

        private void GestisciAggiornamentoSensoreSlot(string payload)
        {
            try
            {
                var messaggio = JsonConvert.DeserializeObject<MessaggioSensoreSlot>(payload);
                if (messaggio?.SlotId != null)
                {
                    var eventArgs = new AggiornamentoSensoreSlotEventArgs
                    {
                        SlotId = messaggio.SlotId,
                        ParcheggioId = messaggio.ParcheggioId ?? "",
                        Stato = messaggio.StatoSlot,
                        ColoreLuce = messaggio.ColoreLuce,
                        MezzoId = messaggio.MezzoPresenteId,
                        Timestamp = messaggio.Timestamp
                    };

                    SensoreSlotAggiornato?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore processamento sensore slot");
            }
        }

        private Task GestisciConnessione(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("Client MQTT connesso al broker");
            return Task.CompletedTask;
        }

        private Task GestisciDisconnessione(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("Client MQTT disconnesso: {Reason}", args.Reason);
            return Task.CompletedTask;
        }

        private string EstraiParcheggioIdDaMezzoId(string mezzoId)
        {
            // Implementa logica per estrarre ID parcheggio da ID mezzo
            // Per ora ritorna un parcheggio di default
            return "PARK_CENTRO";
        }
    }
}
