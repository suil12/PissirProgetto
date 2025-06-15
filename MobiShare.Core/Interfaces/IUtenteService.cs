using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IUtenteService
    {
        Task<Utente?> RegistraAsync(string username, string email, string password, TipoUtente tipo);
        Task<Utente?> LoginAsync(string username, string password);
        Task<Utente?> GetUtenteByIdAsync(string id);
        Task<Utente?> GetByIdAsync(string id); // Alias per compatibilità
        Task<bool> UpdateCreditoAsync(string utenteId, decimal importo);
        Task<BuonoSconto?> ConvertiPuntiEcoAsync(string utenteId, int puntiDaConvertire);
        Task<IEnumerable<Corsa>> GetCorseUtenteAsync(string utenteId);
        Task<IEnumerable<BuonoSconto>> GetBuoniScontoUtenteAsync(string utenteId);
        Task<bool> VerificaCreditoSufficienteAsync(string utenteId, decimal importoRichiesto);
    }
}