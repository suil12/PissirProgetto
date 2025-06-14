using MobiShare.Core.Enums;

namespace MobiShare.API.DTOs
{
    public class MezzoDto
    {
        public string Id { get; set; } = string.Empty;
        public TipoMezzo Tipo { get; set; }
        public string Modello { get; set; } = string.Empty;
        public StatoMezzo Stato { get; set; }
        public int? PercentualeBatteria { get; set; }
        public decimal TariffaPerMinuto { get; set; }
        public double Latitudine { get; set; }
        public double Longitudine { get; set; }
        public string? ParcheggioDiPartenzaId { get; set; }
        public string? SlotId { get; set; }
        public DateTime? UltimaManutenzione { get; set; }
    }

    public class CreaMezzoDto
    {
        public TipoMezzo Tipo { get; set; }
        public string Modello { get; set; } = string.Empty;
        public decimal TariffaPerMinuto { get; set; }
        public string ParcheggioId { get; set; } = string.Empty;
    }

    public class AggiornaStatoMezzoDto
    {
        public StatoMezzo Stato { get; set; }
    }

    public class AggiornaBatteriaMezzoDto
    {
        public int PercentualeBatteria { get; set; }
    }
}