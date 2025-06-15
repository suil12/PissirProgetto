
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MobiShare.IoT.Gateway.Models;
using System.Text;
using Newtonsoft.Json;

namespace MobiShare.IoT.Gateway.Services
{
    public class BackendApiService : IBackendApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BackendApiService> _logger;
        private readonly MqttGatewayConfig _config;

        public BackendApiService(
            HttpClient httpClient,
            ILogger<BackendApiService> logger,
            IOptions<MqttGatewayConfig> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;

            _httpClient.BaseAddress = new Uri(_config.BackendApiUrl);
            if (!string.IsNullOrEmpty(_config.BackendApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.BackendApiKey);
            }
        }

        public async Task<bool> UpdateBatteryLevelAsync(string mezzoId, object? batteryData)
        {
            try
            {
                var endpoint = $"/api/mezzi/{mezzoId}/batteria";
                return await PostToBackendAsync(endpoint, batteryData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento batteria per mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> UpdateVehiclePositionAsync(string mezzoId, object? positionData)
        {
            try
            {
                var endpoint = $"/api/mezzi/{mezzoId}/posizione";
                return await PostToBackendAsync(endpoint, positionData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento posizione per mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> UpdateSlotStatusAsync(string slotId, object? statusData)
        {
            try
            {
                var endpoint = $"/api/slots/{slotId}/stato";
                return await PostToBackendAsync(endpoint, statusData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento stato slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> UpdateParkingGatewayStatusAsync(string parcheggioId, object? gatewayData)
        {
            try
            {
                var endpoint = $"/api/parcheggi/{parcheggioId}/gateway";
                return await PostToBackendAsync(endpoint, gatewayData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento gateway parcheggio {ParcheggioId}", parcheggioId);
                return false;
            }
        }

        public async Task<bool> SendMaintenanceAlertAsync(string mezzoId, string alertMessage)
        {
            try
            {
                var endpoint = "/api/maintenance/alert";
                var payload = new
                {
                    MezzoId = mezzoId,
                    Message = alertMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = "maintenance"
                };
                return await PostToBackendAsync(endpoint, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio alert manutenzione per mezzo {MezzoId}", mezzoId);
                return false;
            }
        }

        public async Task<bool> SendSystemNotificationAsync(string message, object? metadata)
        {
            try
            {
                var endpoint = "/api/system/notification";
                var payload = new
                {
                    Message = message,
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow,
                    Source = "IoTGateway"
                };
                return await PostToBackendAsync(endpoint, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio notifica sistema");
                return false;
            }
        }

        private async Task<bool> PostToBackendAsync(string endpoint, object? data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Chiamata API riuscita: {Endpoint}", endpoint);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Chiamata API fallita: {Endpoint} - {StatusCode}",
                        endpoint, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella chiamata API: {Endpoint}", endpoint);
                return false;
            }
        }
    }
}