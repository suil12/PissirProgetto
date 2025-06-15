using Microsoft.AspNetCore.Mvc;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Enums;

namespace MobiShare.API.Controllers
{
    [ApiController]
    [Route("api/iot")]
    public class IoTController : ControllerBase
    {
        private readonly ILogger<IoTController> _logger;
        private readonly IMezzoService _mezzoService;
        private readonly ISlotService _slotService;
        private readonly IParcheggioService _parcheggioService;
        private readonly IMqttService _mqttService;

        public IoTController(
            ILogger<IoTController> logger,
            IMezzoService mezzoService,
            ISlotService slotService,
            IParcheggioService parcheggioService,
            IMqttService mqttService)
        {
            _logger = logger;
            _mezzoService = mezzoService;
            _slotService = slotService;
            _parcheggioService = parcheggioService;
            _mqttService = mqttService;
        }

        /// <summary>
        /// Aggiorna il livello di batteria di un mezzo tramite IoT
        /// </summary>
        [HttpPut("mezzi/{mezzoId}/battery")]
        public async Task<IActionResult> UpdateBatteryLevel(string mezzoId, [FromBody] BatteryUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento batteria ricevuto per mezzo {MezzoId}: {BatteryLevel}%",
                    mezzoId, request.BatteryLevel);

                var result = await _mezzoService.AggiornaBatteriaAsync(mezzoId, request.BatteryLevel);

                if (!result)
                {
                    return NotFound($"Mezzo {mezzoId} non trovato o non supporta batteria");
                }

                // Se batteria < 20%, pubblica notifica di batteria scarica
                if (request.BatteryLevel < 20)
                {
                    await _mqttService.PubblicaEventoBatteriaScaricaAsync(mezzoId, request.BatteryLevel);
                    _logger.LogWarning("Batteria scarica per mezzo {MezzoId}: {BatteryLevel}%", mezzoId, request.BatteryLevel);
                }

                // Pubblica stato batteria aggiornato
                await _mqttService.PubblicaStatoBatteriaAsync(mezzoId, request.BatteryLevel, request.IsCharging);

                return Ok(new { Success = true, BatteryLevel = request.BatteryLevel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento batteria per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Aggiorna lo stato di un mezzo tramite IoT
        /// </summary>
        [HttpPut("mezzi/{mezzoId}/state")]
        public async Task<IActionResult> UpdateVehicleState(string mezzoId, [FromBody] VehicleStateUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento stato ricevuto per mezzo {MezzoId}: {State}",
                    mezzoId, request.State);

                var mezzo = await _mezzoService.GetByIdAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound($"Mezzo {mezzoId} non trovato");
                }

                // Aggiorna stato nel database
                var result = await _mezzoService.ImpostaStatoManutenzioneAsync(
                    mezzoId,
                    request.State == StatoMezzo.Manutenzione
                );

                if (!result)
                {
                    return BadRequest("Impossibile aggiornare stato mezzo");
                }

                // Pubblica stato aggiornato
                await _mqttService.PubblicaStatoMezzoAsync(mezzoId, request.State);

                return Ok(new { Success = true, State = request.State });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento stato per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Aggiorna lo stato di occupazione di uno slot tramite IoT
        /// </summary>
        [HttpPut("slots/{slotId}/status")]
        public async Task<IActionResult> UpdateSlotStatus(string slotId, [FromBody] SlotStatusUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento slot ricevuto per slot {SlotId}: {State}",
                    slotId, request.State);

                var slot = await _slotService.GetByIdAsync(slotId);
                if (slot == null)
                {
                    return NotFound($"Slot {slotId} non trovato");
                }

                // Aggiorna stato slot nel database
                var result = await _slotService.UpdateStatoAsync(slotId, request.State);
                if (!result)
                {
                    return BadRequest("Impossibile aggiornare stato slot");
                }

                // Se lo slot è stato occupato, assegna il mezzo
                if (request.State == StatoSlot.Occupato && !string.IsNullOrEmpty(request.MezzoId))
                {
                    await _slotService.AssegnaMezzoAsync(slotId, request.MezzoId);
                }
                else if (request.State == StatoSlot.Libero)
                {
                    await _slotService.LiberaSlotAsync(slotId);
                }

                // Pubblica stato slot aggiornato
                await _mqttService.PubblicaStatoSlotAsync(slotId, request.State, request.MezzoId);

                // Aggiorna LED del colore appropriato
                var coloreLed = request.State switch
                {
                    StatoSlot.Libero => ColoreLuce.Verde,
                    StatoSlot.Occupato => ColoreLuce.Rosso,
                    StatoSlot.Manutenzione => ColoreLuce.Giallo,
                    _ => ColoreLuce.Blu
                };

                await _mqttService.PubblicaAggiornamentoLedSlotAsync(slotId, coloreLed);

                return Ok(new { Success = true, State = request.State, Color = coloreLed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento slot {SlotId}", slotId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Aggiorna lo stato del gateway di un parcheggio tramite IoT
        /// </summary>
        [HttpPut("parking/{parcheggioId}/gateway")]
        public async Task<IActionResult> UpdateParkingGatewayStatus(string parcheggioId, [FromBody] ParkingGatewayStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento gateway ricevuto per parcheggio {ParcheggioId}: {Status}",
                    parcheggioId, request.Status);

                var parcheggio = await _parcheggioService.GetByIdAsync(parcheggioId);
                if (parcheggio == null)
                {
                    return NotFound($"Parcheggio {parcheggioId} non trovato");
                }

                // Log dello stato del gateway (in futuro potrebbe essere salvato nel DB)
                _logger.LogInformation("Gateway parcheggio {ParcheggioId}: {Status} - {Message}",
                    parcheggioId, request.Status, request.Message);

                // Se ci sono errori critici, invia notifica sistema
                if (request.Status == "ERROR" || request.Status == "OFFLINE")
                {
                    await _mqttService.PubblicaNotificaSistemaAsync(
                        $"Gateway parcheggio {parcheggioId} in errore: {request.Message}",
                        new { parcheggioId, status = request.Status, timestamp = DateTime.UtcNow }
                    );
                }

                return Ok(new { Success = true, Status = request.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento gateway per parcheggio {ParcheggioId}", parcheggioId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Endpoint per ricevere dati generici dai sensori IoT
        /// </summary>
        [HttpPost("sensor-data")]
        public async Task<IActionResult> ReceiveSensorData([FromBody] SensorDataRequest request)
        {
            try
            {
                _logger.LogDebug("Dati sensore ricevuti: {SensorType} da {DeviceId}",
                    request.SensorType, request.DeviceId);

                // Processa i dati in base al tipo di sensore
                switch (request.SensorType.ToUpper())
                {
                    case "BATTERY":
                        if (request.Data.ContainsKey("level") && request.Data.ContainsKey("mezzoId"))
                        {
                            var batteryLevel = Convert.ToInt32(request.Data["level"]);
                            var mezzoId = request.Data["mezzoId"].ToString();
                            await _mezzoService.AggiornaBatteriaAsync(mezzoId, batteryLevel);
                        }
                        break;

                    case "SLOT_OCCUPANCY":
                        if (request.Data.ContainsKey("occupied") && request.Data.ContainsKey("slotId"))
                        {
                            var occupied = Convert.ToBoolean(request.Data["occupied"]);
                            var slotId = request.Data["slotId"].ToString();
                            var stato = occupied ? StatoSlot.Occupato : StatoSlot.Libero;
                            await _slotService.UpdateStatoAsync(slotId, stato);
                        }
                        break;

                    default:
                        _logger.LogWarning("Tipo sensore non riconosciuto: {SensorType}", request.SensorType);
                        break;
                }

                return Ok(new { Success = true, Processed = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento dati sensore");
                return StatusCode(500, "Errore interno del server");
            }
        }
    }

    // Request Models
    public class BatteryUpdateRequest
    {
        public int BatteryLevel { get; set; }
        public bool IsCharging { get; set; } = false;
        public int? TimeRemainingMinutes { get; set; }
    }

    public class VehicleStateUpdateRequest
    {
        public StatoMezzo State { get; set; }
        public string? Message { get; set; }
    }

    public class SlotStatusUpdateRequest
    {
        public StatoSlot State { get; set; }
        public string? MezzoId { get; set; }
        public string? Message { get; set; }
    }

    public class ParkingGatewayStatusRequest
    {
        public string Status { get; set; } = string.Empty; // "ONLINE", "OFFLINE", "ERROR"
        public string? Message { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class SensorDataRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}