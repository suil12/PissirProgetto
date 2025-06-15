using MobiShare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MobiShare.API.DTOs
{
    public class CorsaDto
    {
        public string Id { get; set; } = string.Empty;
        public string UtenteId { get; set; } = string.Empty;
        public string MezzoId { get; set; } = string.Empty;
        public MezzoDto? Mezzo { get; set; }
        public string ParcheggioDiPartenzaId { get; set; } = string.Empty;
        public ParcheggioDto? ParcheggioDiPartenza { get; set; }
        public string? ParcheggioDestinazioneId { get; set; }
        public ParcheggioDto? ParcheggioDestinazione { get; set; }
        public DateTime DataInizio { get; set; }
        public DateTime? DataFine { get; set; }
        public int Durata { get; set; }
        public decimal Costo { get; set; }
        public int PuntiEcoAccumulati { get; set; }
        public StatoCorsa Stato { get; set; }
    }

    public class IniziaCorsaDto
    {
        [Required]
        public string MezzoId { get; set; } = string.Empty;
    }

    public class TerminaCorsaDto
    {
        [Required]
        public string ParcheggioDestinazioneId { get; set; } = string.Empty;
    }
}
