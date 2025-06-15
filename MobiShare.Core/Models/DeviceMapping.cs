using MobiShare.Core.Enums;

namespace MobiShare.Core.Models
{
    /// <summary>
    /// Modello per mappare dispositivi con i loro identificativi
    /// </summary>
    public class DeviceMapping
    {
        public string DeviceId { get; set; } = string.Empty;
        public TipoDispositivo TipoDispositivo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? MezzoId { get; set; } // Per dispositivi su mezzi
        public string? ParcheggioId { get; set; } // Per dispositivi nei parcheggi
        public string? SlotId { get; set; } // Per dispositivi negli slot
        public bool IsAttivo { get; set; } = true;
        public DateTime UltimaAttivita { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Genera un ID dispositivo univoco basato sul tipo e contesto
        /// </summary>
        public static string GenerateDeviceId(TipoDispositivo tipo, string contestoId)
        {
            var prefisso = tipo switch
            {
                TipoDispositivo.SensoreBatteriaMezzo => "BAT",
                TipoDispositivo.AttuatoreBloccoMezzo => "LOCK",
                TipoDispositivo.SensoreGpsMezzo => "GPS",
                TipoDispositivo.SensoreLuceSlot => "LED",
                TipoDispositivo.AttuatoreLuceSlot => "LED_ACT",
                TipoDispositivo.SensoreOccupazioneSlot => "OCC",
                TipoDispositivo.GatewayParcheggio => "GW",
                TipoDispositivo.SensoreMeteo => "METEO",
                TipoDispositivo.CameraSicurezza => "CAM",
                _ => "DEV"
            };

            return $"{prefisso}_{contestoId}_{DateTime.UtcNow.Ticks % 10000}";
        }
    }
}
