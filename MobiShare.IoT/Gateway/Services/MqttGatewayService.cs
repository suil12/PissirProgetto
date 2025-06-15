using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Models;
using MobiShare.Core.Enums;
using MobiShare.IoT.Gateway.Models;

namespace MobiShare.IoT.Gateway.Services
{
    public class MqttGatewayService : IMqttGatewayService
    {
        private readonly ILogger<MqttGatewayService> _logger;
        private readonly MqttGatewayConfig _config;
        private readonly IDeviceMappingService _deviceMapping;
        private readonly IBackendApiService _backendApi;
        private IManagedMqttClient? _mqttClient;
        private readonly Dictionary<string, DateTime> _deviceLastSeen;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public event EventHandler<DeviceDataReceivedEventArgs>? DeviceDataReceived;
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public MqttGatewayService(
            ILogger<MqttGatewayService> logger,
            IOptions<MqttGatewayConfig> config,
            IDeviceMappingService deviceMapping,
            IBackendApiService backendApi)
        {
            _logger = logger;
            _config = config.Value;
            _deviceMapping = deviceMapping;
            _backendApi = backendApi;
            _deviceLastSeen = new Dictionary<string, DateTime>();
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateManagedMqttClient();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(_config.BrokerHost, _config.BrokerPort)
                    .WithClientId(_config.ClientId)
                    .WithCleanSession(_config.CleanSession)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAlivePeriod));

                if (!string.IsNullOrEmpty(_config.Username))
                {
                    clientOptions.WithCredentials(_config.Username, _config.Password);
                }

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions.Build())
                    .Build();

                // Gestori eventi
                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
                _mqttClient.ConnectedAsync += OnConnected;
                _mqttClient.DisconnectedAsync += OnDisconnected;

                await _mqttClient.StartAsync(managedOptions);

                // Sottoscrizione ai topic principali
                await SubscribeToDeviceTopics();

                _logger.LogInformation("MQTT Gateway avviato con successo");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'avvio del MQTT Gateway");
                return false;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.StopAsync();
                    _mqttClient.Dispose();
                    _mqttClient = null;
                }
                _logger.LogInformation("MQTT Gateway fermato");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella chiusura del MQTT Gateway");
            }
        }

        public async Task<bool> PublishToDeviceAsync(string deviceId, object payload)
        {
            if (!IsConnected) return false;

            try
            {
                var topic = _deviceMapping.GenerateTopicForDevice(deviceId);
                var jsonPayload = JsonConvert.SerializeObject(payload);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient!.EnqueueAsync(message);

                _logger.LogDebug("Messaggio inviato al dispositivo {DeviceId} su topic {Topic}", deviceId, topic);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio messaggio al dispositivo {DeviceId}", deviceId);
                return false;
            }
        }

        public async Task<bool> SendCommandToDeviceAsync(string deviceId, string command, object payload)
        {
            if (!IsConnected) return false;

            try
            {
                var topic = _deviceMapping.GenerateCommandTopicForDevice(deviceId);
                var commandPayload = new
                {
                    Command = command,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow,
                    DeviceId = deviceId
                };

                var jsonPayload = JsonConvert.SerializeObject(commandPayload);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient!.EnqueueAsync(message);

                _logger.LogInformation("Comando {Command} inviato al dispositivo {DeviceId}", command, deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio comando al dispositivo {DeviceId}", deviceId);
                return false;
            }
        }

        private async Task SubscribeToDeviceTopics()
        {
            var topics = new[]
            {
                "mobishare/+/+/data",           // Dati generici dispositivi
                "mobishare/mezzi/+/batteria",   // Batteria mezzi
                "mobishare/mezzi/+/posizione",  // Posizione mezzi
                "mobishare/mezzi/+/stato",      // Stato mezzi
                "mobishare/parcheggi/+/slots/+/stato", // Stato slot
                "mobishare/parcheggi/+/gateway", // Gateway parcheggi
                "mobishare/devices/+/status"     // Status dispositivi
            };

            foreach (var topic in topics)
            {
                await _mqttClient!.SubscribeAsync(topic);
                _logger.LogDebug("Sottoscritto al topic: {Topic}", topic);
            }
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            try
            {
                var topic = eventArgs.ApplicationMessage.Topic;
                var payload = eventArgs.ApplicationMessage.ConvertPayloadToString();

                _logger.LogDebug("Ricevuto messaggio su topic {Topic}: {Payload}", topic, payload);

                // Estrai deviceId dal topic
                var deviceId = ExtractDeviceIdFromTopic(topic);
                if (string.IsNullOrEmpty(deviceId))
                {
                    _logger.LogWarning("Impossibile estrarre deviceId dal topic {Topic}", topic);
                    return;
                }

                // Aggiorna ultima attività dispositivo
                _deviceLastSeen[deviceId] = DateTime.UtcNow;

                // Parse del payload
                var data = JsonConvert.DeserializeObject(payload);

                // Emetti evento per processing downstream
                DeviceDataReceived?.Invoke(this, new DeviceDataReceivedEventArgs
                {
                    DeviceId = deviceId,
                    Topic = topic,
                    Data = data ?? new object(),
                    Timestamp = DateTime.UtcNow
                });

                // Routing verso backend basato sul tipo di messaggio
                await RouteToBackend(deviceId, topic, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento del messaggio MQTT");
            }
        }

        private async Task RouteToBackend(string deviceId, string topic, object? data)
        {
            try
            {
                var device = await _deviceMapping.GetDeviceByIdAsync(deviceId);
                if (device == null)
                {
                    _logger.LogWarning("Dispositivo {DeviceId} non trovato nella mappatura", deviceId);
                    return;
                }

                // Routing basato sul tipo di dispositivo
                switch (device.TipoDispositivo)
                {
                    case TipoDispositivo.SensoreBatteriaMezzo:
                        await _backendApi.UpdateBatteryLevelAsync(device.MezzoId!, data);
                        break;

                    case TipoDispositivo.SensoreGpsMezzo:
                        await _backendApi.UpdateVehiclePositionAsync(device.MezzoId!, data);
                        break;

                    case TipoDispositivo.SensoreLuceSlot:
                    case TipoDispositivo.SensoreOccupazioneSlot:
                        await _backendApi.UpdateSlotStatusAsync(device.SlotId!, data);
                        break;

                    case TipoDispositivo.GatewayParcheggio:
                        await _backendApi.UpdateParkingGatewayStatusAsync(device.ParcheggioId!, data);
                        break;

                    default:
                        _logger.LogDebug("Tipo dispositivo {TipoDispositivo} non gestito per il routing",
                            device.TipoDispositivo);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel routing verso backend per dispositivo {DeviceId}", deviceId);
            }
        }

        private string ExtractDeviceIdFromTopic(string topic)
        {
            // Implementazione semplificata - in un caso reale potresti avere logica più complessa
            var parts = topic.Split('/');

            if (topic.Contains("/mezzi/"))
            {
                var mezzoId = parts[2];
                var sensorType = parts[3]; // batteria, posizione, stato

                return sensorType switch
                {
                    "batteria" => $"BAT_{mezzoId}",
                    "posizione" => $"GPS_{mezzoId}",
                    "stato" => $"LOCK_{mezzoId}",
                    _ => $"DEV_{mezzoId}"
                };
            }

            if (topic.Contains("/slots/"))
            {
                var parcheggioId = parts[2];
                var slotId = parts[4];
                return $"LED_{parcheggioId}_{slotId}";
            }

            if (topic.Contains("/gateway"))
            {
                var parcheggioId = parts[2];
                return $"GW_{parcheggioId}";
            }

            // Fallback
            return parts.LastOrDefault() ?? string.Empty;
        }

        private async Task OnConnected(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogInformation("MQTT Gateway connesso al broker");
            await SubscribeToDeviceTopics();
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs eventArgs)
        {
            _logger.LogWarning("MQTT Gateway disconnesso: {Reason}", eventArgs.Reason);
            return Task.CompletedTask;
        }
    }
}
