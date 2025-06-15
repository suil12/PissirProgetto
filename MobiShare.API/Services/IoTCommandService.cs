using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Enums;
using System.Text;
using Newtonsoft.Json;

namespace MobiShare.API.Services
{
    public class IoTCommandService : IIoTCommandService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IoTCommandService> _logger;
        private readonly IoTGatewayConfig _config;

        public IoTCommandService(
            HttpClient httpClient,
            ILogger<IoTCommandService> logger,
            IOptions<IoTGatewayConfig> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;

            _httpClient.BaseAddress = new Uri(_config.GatewayApiUrl);
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
            }
        }

        public async Task<bool> BlockVehicleAsync(string mezzoId)
        {
            try
            {
                var command = new
                {
                    Command = "BLOCK",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/block", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel blocco mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> UnblockVehicleAsync(string mezzoId)
        {
            try
            {
                var command = new
                {
                    Command = "UNBLOCK",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/unblock", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nello sblocco mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> ChangeSlotLightColorAsync(string slotId, ColoreLuce colore)
        {
            try
            {
                var command = new
                {
                    Command = "CHANGE_COLOR",
                    SlotId = slotId,
                    Color = colore.ToString(),
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

        public async Task<bool> RequestVehicleLocationAsync(string mezzoId)
        {
            try
            {
                var command = new
                {
                    Command = "REQUEST_LOCATION",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendCommandToGatewayAsync($"/api/commands/mezzi/{mezzoId}/location", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella richiesta posizione mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> SendMaintenanceCommandAsync(string mezzoId, string command)
        {
            try
            {
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

        private async Task<bool> SendCommandToGatewayAsync(string endpoint, object command)
        {
            try
            {
                var json = JsonConvert.SerializeObject(command);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Comando inviato con successo al gateway: {Endpoint}", endpoint);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Invio comando fallito: {Endpoint} - {StatusCode}",
                        endpoint, response.StatusCode);
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

    // Configurazione per comunicare con il gateway IoT
    public class IoTGatewayConfig
    {
        public string GatewayApiUrl { get; set; } = "http://localhost:5001";
        public string ApiKey { get; set; } = string.Empty;
    }
}