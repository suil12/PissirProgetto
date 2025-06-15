using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using MobiShare.Core.Enums;

namespace MobiShare.API.Services
{
    public interface IIoTCommandService
    {
        Task<bool> UnblockVehicleAsync(string mezzoId, string corsaId = null, string utenteId = null);
        Task<bool> BlockVehicleAsync(string mezzoId, string corsaId = null);
        Task<bool> ChangeSlotLightColorAsync(string slotId, ColoreLuce colore, int intensita = 100);
        Task<bool> SendMaintenanceCommandAsync(string mezzoId, string command);
        Task<bool> RequestBatteryStatusAsync(string mezzoId);
        Task<bool> SendEmergencyStopAsync(string mezzoId);
    }

    public class IoTCommandService : IIoTCommandService
    {
        private readonly ILogger<IoTCommandService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IoTGatewayConfig _config;

        public IoTCommandService(
            ILogger<IoTCommandService> logger,
            HttpClient httpClient,
            IOptions<IoTGatewayConfig> config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config.Value;

            // Configura l'HttpClient per comunicare con il Gateway IoT
            _httpClient.BaseAddress = new Uri(_config.GatewayApiUrl);
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
            }
        }

        public async Task<bool> UnblockVehicleAsync(string mezzoId, string corsaId = null, string utenteId = null)
        {
            try
            {
                _logger.LogInformation("Invio comando sblocco per mezzo {MezzoId}, corsa {CorsaId}", mezzoId, corsaId);

                var command = new
                {
                    Action = "UNLOCK",
                    MezzoId = mezzoId,
                    CorsaId = corsaId,
                    UtenteId = utenteId,
                    Timestamp = DateTime.UtcNow,
                    Type = "COURSE_START"
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/unlock", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nello sblocco mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> BlockVehicleAsync(string mezzoId, string corsaId = null)
        {
            try
            {
                _logger.LogInformation("Invio comando blocco per mezzo {MezzoId}, corsa {CorsaId}", mezzoId, corsaId);

                var command = new
                {
                    Action = "LOCK",
                    MezzoId = mezzoId,
                    CorsaId = corsaId,
                    Timestamp = DateTime.UtcNow,
                    Type = "COURSE_END"
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/lock", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel blocco mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> ChangeSlotLightColorAsync(string slotId, ColoreLuce colore, int intensita = 100)
        {
            try
            {
                _logger.LogInformation("Cambio LED slot {SlotId} a {Colore} con intensità {Intensita}%",
                    slotId, colore, intensita);

                var command = new
                {
                    Command = "CHANGE_COLOR",
                    SlotId = slotId,
                    Color = colore.ToString(),
                    Intensity = intensita,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/slots/{slotId}/light", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel cambio colore slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> SendMaintenanceCommandAsync(string mezzoId, string command)
        {
            try
            {
                _logger.LogInformation("Invio comando manutenzione {Command} per mezzo {MezzoId}", command, mezzoId);

                var commandPayload = new
                {
                    Command = command,
                    MezzoId = mezzoId,
                    Type = "MAINTENANCE",
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/maintenance", commandPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio comando manutenzione al mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> RequestBatteryStatusAsync(string mezzoId)
        {
            try
            {
                _logger.LogDebug("Richiesta status batteria per mezzo {MezzoId}", mezzoId);

                var command = new
                {
                    Command = "REQUEST_BATTERY_STATUS",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/battery-status", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella richiesta status batteria per mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> SendEmergencyStopAsync(string mezzoId)
        {
            try
            {
                _logger.LogWarning("Invio comando STOP EMERGENZA per mezzo {MezzoId}", mezzoId);

                var command = new
                {
                    Command = "EMERGENCY_STOP",
                    MezzoId = mezzoId,
                    Priority = "HIGH",
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/emergency", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio comando emergenza per mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        // ===== METODI AGGIUNTIVI PER GESTIONE SLOT =====

        public async Task<bool> EnableSlotAsync(string slotId)
        {
            try
            {
                var command = new
                {
                    Command = "ENABLE_SLOT",
                    SlotId = slotId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/slots/{slotId}/enable", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'abilitazione slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> DisableSlotAsync(string slotId, string reason = "MAINTENANCE")
        {
            try
            {
                var command = new
                {
                    Command = "DISABLE_SLOT",
                    SlotId = slotId,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/slots/{slotId}/disable", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella disabilitazione slot {SlotId}", slotId);
                return false;
            }
        }

        // ===== METODI AGGIUNTIVI PER PARCHEGGI =====

        public async Task<bool> RequestParkingStatusAsync(string parcheggioId)
        {
            try
            {
                var command = new
                {
                    Command = "REQUEST_STATUS",
                    ParcheggioId = parcheggioId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/parking/{parcheggioId}/status", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella richiesta status parcheggio {ParcheggioId}", parcheggioId);
                return false;
            }
        }

        // ===== METODI BATCH =====

        public async Task<bool> SendBatchCommandAsync(List<BatchCommand> commands)
        {
            try
            {
                _logger.LogInformation("Invio batch di {Count} comandi", commands.Count);

                var batchPayload = new
                {
                    Commands = commands,
                    BatchId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync("/api/commands/batch", batchPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio batch comandi");
                return false;
            }
        }

        // ===== METODO BASE PER COMUNICAZIONE CON GATEWAY =====

        private async Task<bool> SendCommandToGatewayAsync(string endpoint, object command)
        {
            try
            {
                var json = JsonConvert.SerializeObject(command, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Invio comando al gateway: {Endpoint} - {Command}", endpoint, json);

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Comando inviato con successo: {Endpoint} - Response: {Response}",
                        endpoint, responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Invio comando fallito: {Endpoint} - {StatusCode} - {Error}",
                        endpoint, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio comando al gateway: {Endpoint}", endpoint);
                return false;
            }
        }
    }

    // ===== CONFIGURAZIONE E MODELLI =====

    public class IoTGatewayConfig
    {
        public string GatewayApiUrl { get; set; } = "http://localhost:5001";
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
    }

    public class BatchCommand
    {
        public string Type { get; set; } = string.Empty; // "VEHICLE", "SLOT", "PARKING"
        public string EntityId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public int Priority { get; set; } = 1; // 1 = bassa, 5 = alta
    }

    public class CommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? CommandId { get; set; }
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
    }
}