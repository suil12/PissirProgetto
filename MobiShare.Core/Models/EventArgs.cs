using MobiShare.Core.Enums;

namespace MobiShare.Core.Models
{
    public class MqttMessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

namespace MobiShare.Core.Interfaces
{
    public class AggiornamentoStatoMezzoEventArgs : EventArgs
    {
        public string MezzoId { get; set; } = string.Empty;
        public string ParcheggioId { get; set; } = string.Empty;
        public StatoMezzo Stato { get; set; }
        public int? PercentualeBatteria { get; set; }
        public double? Latitudine { get; set; }
        public double? Longitudine { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AggiornamentoSensoreSlotEventArgs : EventArgs
    {
        public string SlotId { get; set; } = string.Empty;
        public string ParcheggioId { get; set; } = string.Empty;
        public StatoSlot Stato { get; set; }
        public ColoreLuce ColoreLuce { get; set; }
        public string? MezzoId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}