using MobiShare.Core.Enums;

namespace MobiShare.Core.Entities
{
    public class Corsa
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public DateTime DataInizio { get; set; } = DateTime.UtcNow;
        public DateTime? DataFine { get; set; }
        
        public int Durata => DataFine.HasValue ? 
            (int)(DataFine.Value - DataInizio).TotalMinutes : 0;
        
        public decimal Costo { get; set; } = 0;
        
        public int PuntiEcoAccumulati { get; set; } = 0;
        
        public StatoCorsa Stato { get; set; } = StatoCorsa.InCorso;
        
        // Foreign Keys
        public string UtenteId { get; set; } = string.Empty;
        public string MezzoId { get; set; } = string.Empty;
        public string ParcheggioDiPartenzaId { get; set; } = string.Empty;
        public string? ParcheggioDestinazioneId { get; set; }
        
        // Navigation Properties
        public virtual Utente Utente { get; set; } = null!;
        public virtual Mezzo Mezzo { get; set; } = null!;
        public virtual Parcheggio ParcheggioDiPartenza { get; set; } = null!;
        public virtual Parcheggio? ParcheggioDestinazione { get; set; }
    }
}
