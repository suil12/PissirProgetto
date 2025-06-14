using MobiShare.Core.Enums;

namespace MobiShare.Core.Entities
{
    public class Slot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public int Numero { get; set; }
        
        public StatoSlot Stato { get; set; } = StatoSlot.Libero;
        
        public DateTime UltimoAggiornamento { get; set; } = DateTime.UtcNow;
        
        // Foreign Keys
        public string ParcheggiId { get; set; } = string.Empty;
        public string? MezzoId { get; set; }
        
        // Navigation Properties
        public virtual Parcheggio Parcheggio { get; set; } = null!;
        public virtual Mezzo? MezzoPresente { get; set; }
        public virtual DatiSensore? SensoreLuce { get; set; }
    }
}
