using MobiShare.Core.Enums;

namespace MobiShare.Core.Entities
{
    public class DatiSensore
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public ColoreLuce Colore { get; set; } = ColoreLuce.Verde;

        public DateTime UltimaLettura { get; set; } = DateTime.UtcNow;
        //public DateTime UltimoAggiornamento { get; set; } = DateTime.UtcNow; 
        public bool StatoAttivo { get; set; } = true;

        // Foreign Key
        public string SlotId { get; set; } = string.Empty;

        // Navigation Property
        public virtual Slot Slot { get; set; } = null!;
    }
}