using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface ICorsaRepository : IRepository<Corsa>
    {
        Task<IEnumerable<Corsa>> GetByUtenteAsync(string utenteId);
        Task<IEnumerable<Corsa>> GetByStatoAsync(StatoCorsa stato);
        Task<Corsa?> GetCorsaAttivaByUtenteAsync(string utenteId);
        Task<IEnumerable<Corsa>> GetByIntervalloDatiAsync(DateTime dataInizio, DateTime dataFine);
        Task<decimal> GetRicaviTotaliAsync(DateTime? dataInizio = null, DateTime? dataFine = null);
    }
}