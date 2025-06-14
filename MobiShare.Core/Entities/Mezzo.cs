using MobiShare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MobiShare.Core.Entities
{
    public class Mezzo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public TipoMezzo Tipo { get; set; }
        
        [Required, MaxLength(100)]
        public string Modello { get; set; } = string.Empty;
        
        public StatoMezzo Stato { get; set; } = StatoMezzo.Disponibile;
        
        public int? PercentualeBatteria { get; set; } // null per bici muscolari
        
        public decimal TariffaPerMinuto { get; set; }
        
        public double Latitudine { get; set; }
        public double Longitudine { get; set; }
        
        public DateTime? UltimaManutenzione { get; set; }
        
        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
        
        // Foreign Keys
        public string? ParcheggioDiPartenzaId { get; set; }
        public string? SlotId { get; set; }
        
        // Navigation Properties
        public virtual Parcheggio? ParcheggioDiPartenza { get; set; }
        public virtual Slot? Slot { get; set; }
        public virtual ICollection<Corsa> Corse { get; set; } = new List<Corsa>();
    }
}