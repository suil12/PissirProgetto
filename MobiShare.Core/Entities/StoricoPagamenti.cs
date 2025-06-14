namespace MobiShare.Core.Entities
{
    public class StoricoPagementi
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public decimal Importo { get; set; }
        
        public string Tipo { get; set; } = string.Empty; // "Ricarica", "Corsa", "BuonoSconto"
        
        public string? Descrizione { get; set; }
        
        public DateTime DataTransazione { get; set; } = DateTime.UtcNow;
        
        // Foreign Keys
        public string UtenteId { get; set; } = string.Empty;
        public string? CorsaId { get; set; }
        public string? BuonoScontoId { get; set; }
        
        // Navigation Properties
        public virtual Utente Utente { get; set; } = null!;
        public virtual Corsa? Corsa { get; set; }
        public virtual BuonoSconto? BuonoSconto { get; set; }
    }
}