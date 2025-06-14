using MobiShare.Core.Enums;

namespace MobiShare.API.DTOs
{
    public class ParcheggioDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public double Latitudine { get; set; }
        public double Longitudine { get; set; }
        public int Capacita { get; set; }
        public int SlotsDisponibili { get; set; }
        public List<SlotDto> Slots { get; set; } = new();
        public List<MezzoDto> MezziPresenti { get; set; } = new();
    }

    public class CreaParcheggioDto
    {
        public string Nome { get; set; } = string.Empty;
        public double Latitudine { get; set; }
        public double Longitudine { get; set; }
        public int Capacita { get; set; }
    }

    public class SlotDto
    {
        public string Id { get; set; } = string.Empty;
        public int Numero { get; set; }
        public StatoSlot Stato { get; set; }
        public ColoreLuce ColoreLED { get; set; }
        public string? MezzoId { get; set; }
        public DateTime UltimoAggiornamento { get; set; }
    }
}

// MobiShare.API/DTOs/CorsaDto.cs
using MobiShare.Core.Enums;

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
        public string MezzoId { get; set; } = string.Empty;
    }

    public class TerminaCorsaDto
    {
        public string ParcheggioDestinazioneId { get; set; } = string.Empty;
    }
}