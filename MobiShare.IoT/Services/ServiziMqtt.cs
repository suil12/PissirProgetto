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
using MobiShare.IoT.Models;

namespace MobiShare.IoT.Services
{
    public class ServizioMqtt : IMqttService
    {
        private readonly ILogger<ServizioMqtt> _logger;
        private readonly MqttConfig _config;
        private IManagedMqttClient? _clientMqtt;

        public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<EventArgs>? Disconnected;
        public event EventHandler<AggiornamentoStatoMezzoEventArgs>? StatoMezzoAggiornato;
        public event EventHandler<AggiornamentoSensoreSlotEventArgs>? SensoreSlotAggiornato;

        public bool IsConnected => _clientMqtt?.IsConnected ?? false;

        public ServizioMqtt(ILogger<ServizioMqtt> logger, IOptions<MqttConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var mqttFactory = new MqttFactory();
                _clientMqtt = mqttFactory.CreateManagedMqttClient();

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_config.Server, _config.Port)
                    .WithClientId(_config.ClientId)
                    .WithCleanSession(_config.CleanSession);

                if (!string.IsNullOrEmpty(_config.Username))
                {
                    optionsBuilder.WithCredentials(_config.Username, _config.Password);
                }

                var options = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(optionsBuilder.Build())
                    .Build();

                _clientMqtt.ApplicationMessageReceivedAsync += OnMessageReceived;
                _clientMqtt.ConnectedAsync += OnConnected;
                _clientMqtt.DisconnectedAsync += OnDisconnected;

                await _clientMqtt.StartAsync(options);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la connessione MQTT");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_clientMqtt != null)
            {
                await _clientMqtt.StopAsync();
                _clientMqtt.Dispose();
                _clientMqtt = null;
            }
        }

        public async Task<bool> PublishAsync(string topic, object payload)
        {
            if (_clientMqtt == null || !IsConnected)
                return false;

            try
            {
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _clientMqtt.EnqueueAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pubblicazione del messaggio MQTT");
                return false;
            }
        }

        public async Task<bool> SubscribeAsync(string topic)
        {
            if (_clientMqtt == null || !IsConnected)
                return false;

            try
            {
                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _clientMqtt.SubscribeAsync(new[] { topicFilter });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la sottoscrizione MQTT");
                return false;
            }
        }

        public async Task<bool> UnsubscribeAsync(string topic)
        {
            if (_clientMqtt == null || !IsConnected)
                return false;

            try
            {
                await _clientMqtt.UnsubscribeAsync(topic);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la rimozione sottoscrizione MQTT");
                return false;
            }
        }

        public async Task<bool> PubblicaComandoMezzoAsync(string mezzoId, string comando, object payload)
        {
            var topic = $"mobishare/mezzi/{mezzoId}/comandi";
            var message = new
            {
                Comando = comando,
                Timestamp = DateTime.UtcNow,
                Payload = payload
            };

            return await PublishAsync(topic, message);
        }

        public async Task<bool> PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore)
        {
            var topic = $"mobishare/parcheggi/{parcheggioId}/slots/{slotId}/led";
            var message = new
            {
                ColoreLuce = colore.ToString(),
                Timestamp = DateTime.UtcNow
            };

            return await PublishAsync(topic, message);
        }

        public async Task<bool> PubblicaNotificaSistemaAsync(string messaggio, object? dati = null)
        {
            var topic = "mobishare/sistema/notifiche";
            var message = new
            {
                Messaggio = messaggio,
                Dati = dati,
                Timestamp = DateTime.UtcNow
            };

            return await PublishAsync(topic, message);
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var eventArgs = new MqttMessageReceivedEventArgs
                {
                    Topic = e.ApplicationMessage.Topic,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                };

                MessageReceived?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del messaggio MQTT");
            }

            return Task.CompletedTask;
        }

        private Task OnConnected(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("MQTT Client connesso");
            Connected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("MQTT Client disconnesso: {Reason}", e.Reason);
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}
