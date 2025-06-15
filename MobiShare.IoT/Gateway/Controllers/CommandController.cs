using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Interfaces;
using MobiShare.IoT.Gateway.Services;

namespace MobiShare.IoT.Gateway.Controllers
{
    /// <summary>
    /// Controller del microservizio MQTT per ricevere comandi dal backend
    /// </summary>
    [ApiController]
    [Route("api/commands")]
    public class CommandController : ControllerBase
    {
        private readonly IMqttGatewayService _gatewayService;
        private readonly IDeviceMappingService _deviceMapping;
        private readonly ILogger<CommandController> _logger;

        public CommandController(
            IMqttGatewayService gatewayService,
            IDeviceMappingService deviceMapping,
            ILogger<CommandController> logger)
        {
            _gatewayService = gatewayService;
            _deviceMapping = deviceMapping;
            _logger = logger;
        }

        /// <summary>
        /// Riceve comando di sblocco mezzo dal backend
        /// </summary>
        [HttpPost("mezzi/{mezzoId}/unblock")]
        public async Task<IActionResult> UnblockVehicle(string mezzoId, [FromBody] UnblockCommandRequest request)
        {
            try
            {
                _logger.LogInformation("Comando sblocco ricevuto per mezzo {MezzoId}", mezzoId);

                // Trova il dispositivo di blocco per questo mezzo
                var lockDeviceId = $"LOCK_{mezzoId}"; // Usa la convenzione di naming
                var device = await _deviceMapping.GetDeviceByIdAsync(lockDeviceId);

                if (device == null)
                {
                    _logger.LogWarning("Dispositivo blocco non trovato per mezzo {MezzoId}", mezzoId);
                    return NotFound($"Dispositivo blocco non trovato per mezzo {mezzoId}");
                }

                // Invia comando MQTT al dispositivo
                var commandPayload = new
                {
                    Action = "UNLOCK",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow,
                    RequestId = request.RequestId ?? Guid.NewGuid().ToString()
                };

                var success = await _gatewayService.SendCommandToDeviceAsync(lockDeviceId, "UNLOCK", commandPayload);

                if (success)
                {
                    _logger.LogInformation("Comando sblocco inviato con successo per mezzo {MezzoId}", mezzoId);
                    return Ok(new { Success = true, Message = "Comando sblocco inviato", DeviceId = lockDeviceId });
                }
                else
                {
                    _logger.LogError("Fallimento invio comando sblocco per mezzo {MezzoId}", mezzoId);
                    return StatusCode(500, "Errore nell'invio del comando");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento comando sblocco per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve comando di blocco mezzo dal backend
        /// </summary>
        [HttpPost("mezzi/{mezzoId}/block")]
        public async Task<IActionResult> BlockVehicle(string mezzoId, [FromBody] BlockCommandRequest request)
        {
            try
            {
                _logger.LogInformation("Comando blocco ricevuto per mezzo {MezzoId}", mezzoId);

                var lockDeviceId = $"LOCK_{mezzoId}";
                var device = await _deviceMapping.GetDeviceByIdAsync(lockDeviceId);

                if (device == null)
                {
                    return NotFound($"Dispositivo blocco non trovato per mezzo {mezzoId}");
                }

                var commandPayload = new
                {
                    Action = "LOCK",
                    MezzoId = mezzoId,
                    Timestamp = DateTime.UtcNow,
                    RequestId = request.RequestId ?? Guid.NewGuid().ToString()
                };

                var success = await _gatewayService.SendCommandToDeviceAsync(lockDeviceId, "LOCK", commandPayload);

                if (success)
                {
                    _logger.LogInformation("Comando blocco inviato con successo per mezzo {MezzoId}", mezzoId);
                    return Ok(new { Success = true, Message = "Comando blocco inviato", DeviceId = lockDeviceId });
                }
                else
                {
                    return StatusCode(500, "Errore nell'invio del comando");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento comando blocco per mezzo {MezzoId}", mezzoId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Riceve comando cambio colore LED slot
        /// </summary>
        [HttpPost("slots/{slotId}/light")]
        public async Task<IActionResult> ChangeSlotLight(string slotId, [FromBody] LightCommandRequest request)
        {
            try
            {
                _logger.LogInformation("Comando cambio LED ricevuto per slot {SlotId}: {Color}", slotId, request.Color);

                var ledDeviceId = $"LED_ACT_{slotId}"; // Device attuatore LED
                var device = await _deviceMapping.GetDeviceByIdAsync(ledDeviceId);

                if (device == null)
                {
                    return NotFound($"Dispositivo LED non trovato per slot {slotId}");
                }

                var commandPayload = new
                {
                    Action = "CHANGE_COLOR",
                    Color = request.Color,
                    SlotId = slotId,
                    Intensity = request.Intensity ?? 100,
                    Timestamp = DateTime.UtcNow
                };

                var success = await _gatewayService.SendCommandToDeviceAsync(ledDeviceId, "CHANGE_COLOR", commandPayload);

                if (success)
                {
                    return Ok(new { Success = true, Message = "Comando LED inviato", DeviceId = ledDeviceId });
                }
                else
                {
                    return StatusCode(500, "Errore nell'invio del comando");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento comando LED per slot {SlotId}", slotId);
                return StatusCode(500, "Errore interno del server");
            }
        }
    }

    // Request models per i comandi
    public class UnblockCommandRequest
    {
        public string? RequestId { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class BlockCommandRequest
    {
        public string? RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class LightCommandRequest
    {
        public string Color { get; set; } = string.Empty;
        public int? Intensity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}