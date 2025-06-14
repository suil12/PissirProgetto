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
        public double? Latitudine { get; set; }
        public double? Longitudine { get; set; }
        public Dictionary<string, object>? DatiSensore { get; set; }
    }

    public class MessaggioComandoMezzo : MessaggioMqtt
    {
        public string Comando { get; set; } = string.Empty; // "SBLOCCA", "BLOCCA", "LOCALIZZA"
        public string? CorsaId { get; set; }
        public int? DurataMassimaMinuti { get; set; }
    }

    public class MessaggioSensoreSlot : MessaggioMqtt
    {
        public StatoSlot StatoSlot { get; set; }
        public ColoreLuce ColoreLuce { get; set; }
        public bool SensoreAttivo { get; set; } = true;
        public string? MezzoPresenteId { get; set; }
    }
}