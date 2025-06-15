using MobiShare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MobiShare.API.DTOs
{



    public class UtenteDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TipoUtente Tipo { get; set; }
        public decimal Credito { get; set; }
        public int PuntiEco { get; set; }
        public StatoUtente Stato { get; set; }
        public DateTime DataRegistrazione { get; set; }
    }

    public class AggiornaCreditoDto
    {
        public decimal Importo { get; set; }
    }

    public class ConvertiPuntiDto
    {
        public int PuntiDaConvertire { get; set; }
    }
}
