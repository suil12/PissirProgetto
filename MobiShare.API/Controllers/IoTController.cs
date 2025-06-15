
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.API.Controllers
{
    /// <summary>
    /// Controller per ricevere dati dal microservizio MQTT Gateway
    /// </summary>
    [ApiController]
    [Route("api/iot")]
    public class IoTController : ControllerBase
    {
        private readonly ILogger<IoTController> _logger;
        private readonly IMezzoService _mezzoService;
        private readonly IParcheggioService _parcheggioService;
        private readonly ISlotService _slotService;
        private readonly INotificationService _notificationService;

        public IoTController(
            ILogger<IoTController> logger,
            IMezzoService mezzoService,
            IParcheggioService parcheggioService,
            ISlotService slotService,
            INotificationService notificationService)
        {
            _logger = logger;
            _mezzoService = mezzoService;
            _parcheggioService = parcheggioService;
            _slotService = slotService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Riceve aggiornamenti batteria dai dispositivi tramite gateway MQTT
        /// </summary>
        [HttpPost("mezzi/{mezzoId}/batteria")]
        public async Task<IActionResult> UpdateBatteryLevel(string mezzoId, [FromBody] BatteryUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento batteria ricevuto per mezzo {MezzoId}: {BatteryLevel}%",
                    mezzoId, request.BatteryLevel);

                var mezzo = await _mezzoService.GetByIdAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound($"Mezzo {mezzoId} non trovato");
                }

                // Aggiorna livello batteria
                mezzo.PercentualeBatteria = request.BatteryLevel;

                // Controlla se la batteria è scarica
                if (request.BatteryLevel <= 20 && mezzo.Stato != StatoMezzo.BatteriaScarica)
                {
                    mezzo.Stato = StatoMezzo.BatteriaScarica;
                    await _notificationService.SendBatteryLowAlertAsync(mezzoId, request.BatteryLevel);
                    _logger.LogWarning("Batteria scarica per mezzo {MezzoId}: {BatteryLevel}%", mezzoId, request.BatteryLevel);
                }
                else if (request.BatteryLevel > 20 && mezzo.Stato == StatoMezzo.BatteriaScarica)
                {
                    mezzo.Stato = StatoMezzo.Disponibile;
                    _logger.LogInformation("Batteria ripristinata per mezzo {MezzoId}: {BatteryLevel}%", mezzoId, request.BatteryLevel);
                }

                await _mezzoService.UpdateAsync(mezzo);

                return Ok(new { Success = true, Message = "Batteria aggiornata con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento batteria per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve aggiornamenti posizione GPS dai mezzi
        /// </summary>
        [HttpPost("mezzi/{mezzoId}/posizione")]
        public async Task<IActionResult> UpdateVehiclePosition(string mezzoId, [FromBody] PositionUpdateRequest request)
        {
            try
            {
                _logger.LogDebug("Aggiornamento posizione ricevuto per mezzo {MezzoId}: {Lat}, {Lng}",
                    mezzoId, request.Latitude, request.Longitude);

                var mezzo = await _mezzoService.GetByIdAsync(mezzoId);
                if (mezzo == null)
                {
                    return NotFound($"Mezzo {mezzoId} non trovato");
                }

                // Aggiorna posizione
                mezzo.Latitudine = request.Latitude;
                mezzo.Longitudine = request.Longitude;

                await _mezzoService.UpdateAsync(mezzo);

                return Ok(new { Success = true, Message = "Posizione aggiornata con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento posizione per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve aggiornamenti stato slot dai sensori parcheggio
        /// </summary>
        [HttpPost("slots/{slotId}/stato")]
        public async Task<IActionResult> UpdateSlotStatus(string slotId, [FromBody] SlotStatusUpdateRequest request)
        {
            try
            {
                _logger.LogDebug("Aggiornamento stato slot {SlotId}: Occupato={IsOccupied}",
                    slotId, request.IsOccupied);

                var slot = await _slotService.GetByIdAsync(slotId);
                if (slot == null)
                {
                    return NotFound($"Slot {slotId} non trovato");
                }

                // Aggiorna stato slot
                var nuovoStato = request.IsOccupied ? StatoSlot.Occupato : StatoSlot.Libero;

                if (slot.Stato != nuovoStato)
                {
                    slot.Stato = nuovoStato;

                    // Aggiorna anche il sensore luce se presente
                    if (slot.SensoreLuce != null)
                    {
                        slot.SensoreLuce.Colore = request.IsOccupied ? ColoreLuce.Rosso : ColoreLuce.Verde;
                        slot.SensoreLuce.UltimaLettura = DateTime.UtcNow;
                    }

                    await _slotService.UpdateAsync(slot);

                    _logger.LogInformation("Slot {SlotId} cambiato stato da {OldStato} a {NewStato}",
                        slotId, slot.Stato, nuovoStato);
                }

                return Ok(new { Success = true, Message = "Stato slot aggiornato con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento stato slot {SlotId}", slotId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve stato gateway parcheggio
        /// </summary>
        [HttpPost("parcheggi/{parcheggioId}/gateway")]
        public async Task<IActionResult> UpdateParkingGatewayStatus(string parcheggioId, [FromBody] GatewayStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Aggiornamento gateway parcheggio {ParcheggioId}: Status={Status}",
                    parcheggioId, request.Status);

                var parcheggio = await _parcheggioService.GetByIdAsync(parcheggioId);
                if (parcheggio == null)
                {
                    return NotFound($"Parcheggio {parcheggioId} non trovato");
                }

                // Log dello stato del gateway per monitoraggio
                // In un'implementazione completa potresti salvare questi dati per analytics

                return Ok(new { Success = true, Message = "Stato gateway registrato" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento gateway parcheggio {ParcheggioId}", parcheggioId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve alert di manutenzione
        /// </summary>
        [HttpPost("maintenance/alert")]
        public async Task<IActionResult> ReceiveMaintenanceAlert([FromBody] MaintenanceAlertRequest request)
        {
            try
            {
                _logger.LogWarning("Alert manutenzione ricevuto per mezzo {MezzoId}: {Message}",
                    request.MezzoId, request.Message);

                await _notificationService.SendMaintenanceAlertAsync(request.MezzoId, request.Message);

                return Ok(new { Success = true, Message = "Alert manutenzione processato" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento alert manutenzione");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve notifiche sistema dal gateway IoT
        /// </summary>
        [HttpPost("system/notification")]
        public async Task<IActionResult> ReceiveSystemNotification([FromBody] SystemNotificationRequest request)
        {
            try
            {
                _logger.LogInformation("Notifica sistema ricevuta: {Message}", request.Message);

                await _notificationService.SendSystemNotificationAsync(request.Message, request.Metadata);

                return Ok(new { Success = true, Message = "Notifica sistema processata" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento notifica sistema");
                return StatusCode(500, "Errore interno del server");
            }
        }
    }

    // Request Models per il controller IoT
    public class BatteryUpdateRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string MezzoId { get; set; } = string.Empty;
        public int BatteryLevel { get; set; }
        public double Voltage { get; set; }
        public int Temperature { get; set; }
        public bool IsCharging { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PositionUpdateRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string MezzoId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Altitude { get; set; }
        public int Speed { get; set; }
        public int Heading { get; set; }
        public int Accuracy { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SlotStatusUpdateRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SlotId { get; set; } = string.Empty;
        public string ParcheggioId { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public string LightColor { get; set; } = string.Empty;
        public int LightIntensity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class GatewayStatusRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string ParcheggioId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public object? Metadata { get; set; }
    }

    public class MaintenanceAlertRequest
    {
        public string MezzoId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class SystemNotificationRequest
    {
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public object? Metadata { get; set; }
    }
}
