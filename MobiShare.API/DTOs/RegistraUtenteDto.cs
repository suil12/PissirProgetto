using System.ComponentModel.DataAnnotations;
using MobiShare.Core.Enums;

namespace MobiShare.API.DTOs
{
    public class RegistraUtenteDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public TipoUtente Tipo { get; set; }
    }
}
