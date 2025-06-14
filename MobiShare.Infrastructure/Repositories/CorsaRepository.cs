using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Data;

namespace MobiShare.Infrastructure.Repositories
{
    public class CorsaRepository : Repository<Corsa>, ICorsaRepository
    {
        public CorsaRepository(MobiShareDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Corsa>> GetByUtenteAsync(string utenteId)
        {
            return await _dbSet
                .Include(c => c.Mezzo)
                .Include(c => c.ParcheggioDiPartenza)
                .Include(c => c.ParcheggioDestinazione)
                .Where(c => c.UtenteId == utenteId)
                .OrderByDescending(c => c.DataInizio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Corsa>> GetByStatoAsync(StatoCorsa stato)
        {
            return await _dbSet
                .Include(c => c.Utente)
                .Include(c => c.Mezzo)
                .Where(c => c.Stato == stato)
                .ToListAsync();
        }

        public async Task<Corsa?> GetCorsaAttivaByUtenteAsync(string utenteId)
        {
            return await _dbSet
                .Include(c => c.Mezzo)
                .Include(c => c.ParcheggioDiPartenza)
                .FirstOrDefaultAsync(c => c.UtenteId == utenteId && c.Stato == StatoCorsa.InCorso);
        }

        public async Task<IEnumerable<Corsa>> GetByIntervalloDatiAsync(DateTime dataInizio, DateTime dataFine)
        {
            return await _dbSet
                .Include(c => c.Utente)
                .Include(c => c.Mezzo)
                .Where(c => c.DataInizio >= dataInizio && c.DataInizio <= dataFine)
                .OrderBy(c => c.DataInizio)
                .ToListAsync();
        }

        public async Task<decimal> GetRicaviTotaliAsync(DateTime? dataInizio = null, DateTime? dataFine = null)
        {
            var query = _dbSet.Where(c => c.Stato == StatoCorsa.Completata);

            if (dataInizio.HasValue)
                query = query.Where(c => c.DataInizio >= dataInizio.Value);

            if (dataFine.HasValue)
                query = query.Where(c => c.DataInizio <= dataFine.Value);

            return await query.SumAsync(c => c.Costo);
        }
    }
}