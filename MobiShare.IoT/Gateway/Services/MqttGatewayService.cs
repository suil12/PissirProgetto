using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.IoT.Gateway.Models;

namespace MobiShare.IoT.Gateway.Services
{
    public class MqttGatewayService : IMqttGatewayService
    {
        private readonly ILogger<MqttGatewayService> _logger;
        private readonly IDeviceMappingService _deviceMapping;
        private readonly IBackendApiService _backendApi;
        private readonly MqttGatewayConfig _config;
        private IManagedMqttClient? _mqttClient;
        private readonly Dictionary<string, DateTime> _deviceLastSeen = new();

        public bool IsConnected => _mqttClient?.IsConnected ?? false;
        public event EventHandler<DeviceDataReceivedEventArgs>? DeviceDataReceived;
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public MqttGatewayService(
            ILogger<MqttGatewayService> logger,
            IDeviceMappingService deviceMapping,
            IBackendApiService backendApi,
            IOptions<MqttGatewayConfig> config)
        {
            _logger = logger;
            _deviceMapping = deviceMapping;
            _backendApi = backendApi;
            _config = config.Value;
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateManagedMqttClient();

                var clientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(_config.ClientId)
                    .WithTcpServer(_config.BrokerHost, _config.BrokerPort)
                    .WithCleanSession(_config.CleanSession)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAlivePeriod));

                // Aggiungi credenziali solo se specificate
                if (!string.IsNullOrEmpty(_config.Username))
                {
                    clientOptionsBuilder.WithCredentials(_config.Username, _config.Password);
                }

                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(clientOptionsBuilder.Build())
                    .Build();

                _mqttClient.ConnectedAsync += OnConnected;
                _mqttClient.DisconnectedAsync += OnDisconnected;
                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

                await _mqttClient.StartAsync(options);
                await SubscribeToAllTopics();

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

        private async Task SubscribeToAllTopics()
        {
            var topics = new[]
            {
                // Comandi dal backend ai dispositivi
                "mobishare/commands/mezzi/+/unlock",
                "mobishare/commands/mezzi/+/lock",
                "mobishare/commands/slots/+/led",
                "mobishare/commands/mezzi/+/maintenance",
                
                // Status dai dispositivi al backend
                "mobishare/status/mezzi/+/battery",
                "mobishare/status/mezzi/+/state",
                "mobishare/status/slots/+/occupancy",
                "mobishare/status/parking/+/gateway",
                
                // Eventi di sistema bidirezionali
                "mobishare/events/corse/start",
                "mobishare/events/corse/end",
                "mobishare/events/mezzi/battery_low",
                "mobishare/events/mezzi/maintenance_needed"
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

                // Determina il tipo di messaggio basato sul topic
                if (topic.StartsWith("mobishare/commands/"))
                {
                    await HandleCommand(topic, payload);
                }
                else if (topic.StartsWith("mobishare/status/"))
                {
                    await HandleStatusUpdate(topic, payload);
                }
                else if (topic.StartsWith("mobishare/events/"))
                {
                    await HandleSystemEvent(topic, payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento del messaggio MQTT");
            }
        }

        private async Task HandleCommand(string topic, string payload)
        {
            var parts = topic.Split('/');

            if (parts.Length >= 4)
            {
                var entityType = parts[2]; // mezzi, slots
                var entityId = parts[3];   // ID specifico
                var command = parts.Length > 4 ? parts[4] : "unknown";

                _logger.LogInformation("Comando ricevuto: {Command} per {EntityType} {EntityId}", command, entityType, entityId);

                // Aggiorna ultima attività dispositivo
                var deviceId = GenerateDeviceId(entityType, entityId, command);
                _deviceLastSeen[deviceId] = DateTime.UtcNow;

                // Simula esecuzione comando fisico (in un caso reale, comunicheresti con i dispositivi)
                await SimulatePhysicalCommand(entityType, entityId, command, payload);

                // Invia conferma al backend
                await SendCommandConfirmation(entityType, entityId, command, true);
            }
        }

        private async Task HandleStatusUpdate(string topic, string payload)
        {
            var parts = topic.Split('/');

            if (parts.Length >= 4)
            {
                var entityType = parts[2]; // mezzi, slots, parking
                var entityId = parts[3];   // ID specifico
                var statusType = parts.Length > 4 ? parts[4] : "unknown";

                _logger.LogDebug("Status update ricevuto: {StatusType} per {EntityType} {EntityId}", statusType, entityType, entityId);

                // Parse del payload
                var data = JsonConvert.DeserializeObject(payload);

                // Emetti evento per processing downstream
                DeviceDataReceived?.Invoke(this, new DeviceDataReceivedEventArgs
                {
                    DeviceId = GenerateDeviceId(entityType, entityId, statusType),
                    Topic = topic,
                    Data = data ?? new object(),
                    Timestamp = DateTime.UtcNow
                });

                // Routing verso backend
                await RouteToBackend(entityType, entityId, statusType, data);
            }
        }

        private async Task HandleSystemEvent(string topic, string payload)
        {
            _logger.LogInformation("Evento di sistema ricevuto su topic {Topic}: {Payload}", topic, payload);

            // Gli eventi di sistema potrebbero richiedere azioni specifiche
            // Per ora li logghiamo, ma in futuro potrebbero triggerare automazioni
        }

        private async Task RouteToBackend(string entityType, string entityId, string statusType, object? data)
        {
            try
            {
                switch (entityType)
                {
                    case "mezzi":
                        switch (statusType)
                        {
                            case "battery":
                                await _backendApi.UpdateBatteryLevelAsync(entityId, data);
                                break;
                            case "state":
                                await _backendApi.UpdateVehiclePositionAsync(entityId, data);
                                break;
                        }
                        break;

                    case "slots":
                        switch (statusType)
                        {
                            case "occupancy":
                                await _backendApi.UpdateSlotStatusAsync(entityId, data);
                                break;
                        }
                        break;

                    case "parking":
                        switch (statusType)
                        {
                            case "gateway":
                                await _backendApi.UpdateParkingGatewayStatusAsync(entityId, data);
                                break;
                        }
                        break;

                    default:
                        _logger.LogDebug("Tipo entità {EntityType} non gestito per il routing", entityType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel routing verso backend per {EntityType} {EntityId}", entityType, entityId);
            }
        }

        private async Task SimulatePhysicalCommand(string entityType, string entityId, string command, string payload)
        {
            // Simula l'esecuzione del comando fisico
            await Task.Delay(100); // Simula latenza dispositivo

            switch (entityType)
            {
                case "mezzi":
                    switch (command)
                    {
                        case "unlock":
                            _logger.LogInformation("🔓 Mezzo {MezzoId} sbloccato fisicamente", entityId);
                            break;
                        case "lock":
                            _logger.LogInformation("🔒 Mezzo {MezzoId} bloccato fisicamente", entityId);
                            break;
                        case "maintenance":
                            _logger.LogInformation("🔧 Mezzo {MezzoId} impostato in manutenzione", entityId);
                            break;
                    }
                    break;

                case "slots":
                    if (command == "led")
                    {
                        try
                        {
                            var commandData = JsonConvert.DeserializeObject<dynamic>(payload);
                            var color = commandData?.Color?.ToString() ?? "unknown";
                            _logger.LogInformation("💡 LED slot {SlotId} cambiato a {Color}", (object)entityId, (object)color);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Errore nel parsing del comando LED per slot {SlotId}", entityId);
                        }
                    }
                    break;
            }
        }

        private async Task SendCommandConfirmation(string entityType, string entityId, string command, bool success)
        {
            var confirmationTopic = $"mobishare/status/{entityType}/{entityId}/command_result";
            var confirmation = new
            {
                Command = command,
                Success = success,
                Timestamp = DateTime.UtcNow,
                EntityType = entityType,
                EntityId = entityId
            };

            await PublishAsync(confirmationTopic, confirmation);
        }

        public async Task<bool> SendCommandToDeviceAsync(string deviceId, string command, object payload)
        {
            if (!IsConnected) return false;

            try
            {
                var topic = GenerateCommandTopicForDevice(deviceId);
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

        public async Task<bool> PublishAsync(string topic, object payload)
        {
            if (!IsConnected) return false;

            try
            {
                var jsonPayload = JsonConvert.SerializeObject(payload);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient!.EnqueueAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio messaggio MQTT");
                return false;
            }
        }

        public async Task<bool> PublishToDeviceAsync(string deviceId, object payload)
        {
            if (!IsConnected) return false;

            try
            {
                var topic = _deviceMapping.GenerateTopicForDevice(deviceId);
                return await PublishAsync(topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella pubblicazione al dispositivo {DeviceId}", deviceId);
                return false;
            }
        }

        private string GenerateDeviceId(string entityType, string entityId, string operation)
        {
            return operation switch
            {
                "battery" => $"BAT_{entityId}",
                "unlock" or "lock" or "state" => $"LOCK_{entityId}",
                "led" or "occupancy" => $"LED_{entityId}",
                "gateway" => $"GW_{entityId}",
                "maintenance" => $"MAINT_{entityId}",
                _ => $"DEV_{entityId}"
            };
        }

        private string GenerateCommandTopicForDevice(string deviceId)
        {
            // Estrae il tipo di dispositivo dall'ID per generare il topic corretto
            if (deviceId.StartsWith("LOCK_"))
            {
                var mezzoId = deviceId.Substring(5);
                return $"mobishare/commands/mezzi/{mezzoId}/device";
            }
            else if (deviceId.StartsWith("LED_"))
            {
                var slotId = deviceId.Substring(4);
                return $"mobishare/commands/slots/{slotId}/device";
            }

            return $"mobishare/commands/device/{deviceId}";
        }

        private Task OnConnected(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("MQTT Gateway connesso al broker");
            return Task.CompletedTask;
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("MQTT Gateway disconnesso: {Reason}", e.Reason);
            return Task.CompletedTask;
        }
    }
}