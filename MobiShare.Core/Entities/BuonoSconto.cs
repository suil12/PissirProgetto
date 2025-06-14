using MobiShare.Core.Enums;

namespace MobiShare.Core.Entities
{
    public class BuonoSconto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public decimal Valore { get; set; }
        
        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
        public DateTime DataScadenza { get; set; }
        
        public StatoBuono Stato { get; set; } = StatoBuono.Valido;
        
        // Foreign Key
        public string UtenteId { get; set; } = string.Empty;
        
        // Navigation Property
        public virtual Utente Utente { get; set; } = null!;
    }
}
