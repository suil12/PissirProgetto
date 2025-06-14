using MobiShare.Core.Enums;

namespace MobiShare.API.DTOs
{
    public class RegistraUtenteDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserType Tipo { get; set; }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UtenteDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserType Tipo { get; set; }
        public decimal Credito { get; set; }
        public int PuntiEco { get; set; }
        public UserStatus Stato { get; set; }
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
