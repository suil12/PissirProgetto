using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IUtenteRepository : IRepository<Utente>
    {
        Task<Utente?> GetByUsernameAsync(string username);
        Task<Utente?> GetByEmailAsync(string email);
        Task<IEnumerable<Utente>> GetByTypeAsync(UserType type);
        Task<bool> UpdateCreditoAsync(string utenteId, decimal nuovoCredito);
        Task<bool> UpdatePuntiEcoAsync(string utenteId, int nuoviPunti);
    }
}