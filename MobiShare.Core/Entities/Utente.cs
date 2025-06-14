using MobiShare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MobiShare.Core.Entities
{
    public class Utente
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public UserType Tipo { get; set; }
        
        public decimal Credito { get; set; } = 0;
        
        public int PuntiEco { get; set; } = 0;
        
        public UserStatus Stato { get; set; } = UserStatus.Attivo;
        
        public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Corsa> Corse { get; set; } = new List<Corsa>();
        public virtual ICollection<BuonoSconto> BuoniSconto { get; set; } = new List<BuonoSconto>();
        public virtual ICollection<StoricoPagementi> StoricoPagementi { get; set; } = new List<StoricoPagementi>();
    }
}