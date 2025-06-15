using System.ComponentModel.DataAnnotations;

namespace MobiShare.Core.Entities
{
    public class Parcheggio
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Indirizzo { get; set; } = string.Empty;

        public double Latitudine { get; set; }
        public double Longitudine { get; set; }

        public int Capacita { get; set; }

        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Slot> Slots { get; set; } = new List<Slot>();
        public virtual ICollection<Mezzo> MezziPresenti { get; set; } = new List<Mezzo>();
        public virtual ICollection<Corsa> CorsePartenza { get; set; } = new List<Corsa>();
        public virtual ICollection<Corsa> CorseDestinazione { get; set; } = new List<Corsa>();
    }
}