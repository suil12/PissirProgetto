using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace MobiShare.Infrastructure.Services
{
    /// <summary>
    /// Implementazione HTTP del servizio MQTT che comunica con il Gateway IoT
    /// </summary>
    public class HttpMqttService : IMqttService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpMqttService> _logger;
        private readonly string _gatewayBaseUrl;

        public HttpMqttService(HttpClient httpClient, IConfiguration configuration, ILogger<HttpMqttService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _gatewayBaseUrl = configuration["IoTGateway:BaseUrl"] ?? "http://localhost:5001";

            _httpClient.BaseAddress = new Uri(_gatewayBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MobiShare-Backend");
        }

        public async Task<bool> PubblicaComandoMezzoAsync(string mezzoId, string comando, object? parametri = null)
        {
            try
            {
                var request = new
                {
                    MezzoId = mezzoId,
                    Comando = comando,
                    Parametri = parametri,
                    Timestamp = DateTime.UtcNow
                };

                return await SendPostRequestAsync("/api/commands/vehicle", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio comando mezzo {MezzoId}: {Comando}", mezzoId, comando);
                return false;
            }
        }

        public async Task<bool> PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore)
        {
            try
            {
                var request = new
                {
                    SlotId = slotId,
                    ParcheggioId = parcheggioId,
                    Colore = colore.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                return await SendPostRequestAsync("/api/commands/slot", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> PubblicaNotificaSistemaAsync(string messaggio, string? mezzoId = null)
        {
            try
            {
                var request = new
                {
                    Messaggio = messaggio,
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow
                };

                return await SendPostRequestAsync("/api/notifications/system", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio notifica sistema: {Messaggio}", messaggio);
                return false;
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gateway IoT non raggiungibile");
                return false;
            }
        }

        private async Task<bool> SendPostRequestAsync(string endpoint, object request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Comando inviato al gateway: {Endpoint}", endpoint);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gateway ha rifiutato il comando: {Endpoint} - {StatusCode} - {Error}",
                        endpoint, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella comunicazione col gateway: {Endpoint}", endpoint);
                return false;
            }
        }

        public async Task<bool> PubblicaEventoBatteriaScaricaAsync(string mezzoId, int percentualeBatteria)
        {
            return await PubblicaNotificaSistemaAsync($"Batteria scarica per mezzo {mezzoId}: {percentualeBatteria}%", mezzoId);
        }

        public async Task<bool> PubblicaStatoBatteriaAsync(string mezzoId, int percentualeBatteria, bool isCharging)
        {
            var payload = new { mezzoId, percentualeBatteria, isCharging, timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync($"/api/iot/mezzi/{mezzoId}/battery", payload);
        }

        public async Task<bool> PubblicaStatoMezzoAsync(string mezzoId, StatoMezzo stato)
        {
            var payload = new { mezzoId, stato = stato.ToString(), timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync($"/api/iot/mezzi/{mezzoId}/state", payload);
        }

        public async Task<bool> PubblicaStatoSlotAsync(string slotId, StatoSlot stato, string? mezzoId)
        {
            var payload = new { slotId, stato = stato.ToString(), mezzoId, timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync($"/api/iot/slots/{slotId}/status", payload);
        }

        public async Task<bool> PubblicaAggiornamentoLedSlotAsync(string slotId, ColoreLuce colore)
        {
            var payload = new { slotId, colore = colore.ToString(), timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync($"/api/iot/slots/{slotId}/led", payload);
        }

        public async Task<bool> PubblicaNotificaSistemaAsync(string messaggio, object? datiAggiuntivi)
        {
            var payload = new { messaggio, dati = datiAggiuntivi, timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync("/api/iot/system/notification", payload);
        }

        public async Task<bool> PubblicaEventoInizioCorsaAsync(string corsaId, string mezzoId, string utenteId)
        {
            var payload = new { corsaId, mezzoId, utenteId, timestamp = DateTime.UtcNow };
            return await SendPostRequestAsync($"/api/iot/corse/{corsaId}/start", payload);
        }

        public async Task<bool> PubblicaEventoFineCorsaAsync(string corsaId, string mezzoId, TimeSpan durata, decimal costo)
        {
            var payload = new
            {
                corsaId,
                mezzoId,
                durata = durata.TotalMinutes,
                costo,
                timestamp = DateTime.UtcNow
            };
            return await SendPostRequestAsync($"/api/iot/corse/{corsaId}/end", payload);
        }
    }
}
