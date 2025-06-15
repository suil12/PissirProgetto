using MobiShare.Core.Enums;

namespace MobiShare.Core.Models
{
    public class MessaggioMqtt
    {
        public MqttMessageType TipoMessaggio { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? MezzoId { get; set; }
        public string? ParcheggioId { get; set; }
        public string? SlotId { get; set; }
        public string? UtenteId { get; set; }
        public object? Dati { get; set; }
    }

    public class MessaggioStatoMezzo : MessaggioMqtt
    {
        public TipoMezzo TipoMezzo { get; set; }
        public StatoMezzo Stato { get; set; }
        public int? PercentualeBatteria { get; set; }
        public Dictionary<string, object>? DatiSensore { get; set; }

        // CAMPI PER GESTIONE CORSE
        public string? CorsaId { get; set; }
        public DateTime? InizioCorsa { get; set; }
        public DateTime? FineCorsa { get; set; }
        public decimal? CostoCorsa { get; set; }
        public int? DurataMinuti { get; set; }

        // CAMPI PER BATTERIA
        public bool IsCharging { get; set; }
        public int? TimeRemainingMinutes { get; set; }
        public string? AlertLevel { get; set; } // "OK", "LOW", "CRITICAL"

        // CAMPI PER MANUTENZIONE
        public DateTime? UltimaManutenzione { get; set; }
        public string? TipoManutenzione { get; set; }
    }

    public class MessaggioComandoMezzo : MessaggioMqtt
    {
        public string Comando { get; set; } = string.Empty; // "UNLOCK", "LOCK", "MAINTENANCE"
        public string? CorsaId { get; set; }
        public int? DurataMassimaMinuti { get; set; }
        public string? RequestId { get; set; }
    }

    public class MessaggioSensoreSlot : MessaggioMqtt
    {
        public StatoSlot StatoSlot { get; set; }
        public ColoreLuce ColoreLuce { get; set; }
        public bool SensoreAttivo { get; set; } = true;
        public string? MezzoPresenteId { get; set; }
        public int? Intensita { get; set; } = 100;
    }

    public class MessaggioEventoCorsa : MessaggioMqtt
    {
        public string CorsaId { get; set; } = string.Empty;
        public TipoEventoCorsa TipoEvento { get; set; }
        public string? ParcheggioPartenzaId { get; set; }
        public string? ParcheggioDestinazioneId { get; set; }
        public string? SlotPartenzaId { get; set; }
        public string? SlotDestinazioneId { get; set; }
        public decimal? Costo { get; set; }
        public int? DurataMinuti { get; set; }
        public int? PercentualeBatteriaInizio { get; set; }
        public int? PercentualeBatteriaFine { get; set; }
    }

    public class MessaggioNotificaSistema : MessaggioMqtt
    {
        public string Messaggio { get; set; } = string.Empty;
        public TipoNotifica Tipo { get; set; }
        public string? Destinatario { get; set; } // ID utente o "ADMIN" per notifiche sistema
        public Dictionary<string, object>? DatiAggiuntivi { get; set; }
    }

    public class MessaggioStatusGateway : MessaggioMqtt
    {
        public string GatewayId { get; set; } = string.Empty;
        public StatusGateway Status { get; set; }
        public string? Messaggio { get; set; }
        public int? DispositiviConnessi { get; set; }
        public DateTime? UltimoHeartbeat { get; set; }
        public Dictionary<string, object>? Diagnostics { get; set; }
    }
}