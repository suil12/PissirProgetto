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
        public string Indirizzo { get; set; } = string.Empty;
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