using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Data;

namespace MobiShare.Infrastructure.Repositories
{
    public class UtenteRepository : Repository<Utente>, IUtenteRepository
    {
        public UtenteRepository(MobiShareDbContext context) : base(context)
        {
        }

        public async Task<Utente?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Utente?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<Utente>> GetByTypeAsync(TipoUtente type)
        {
            return await _dbSet.Where(u => u.Tipo == type).ToListAsync();
        }

        public async Task<bool> UpdateCreditoAsync(string utenteId, decimal nuovoCredito)
        {
            var utente = await GetByIdAsync(utenteId);
            if (utente == null) return false;

            utente.Credito = nuovoCredito;
            await UpdateAsync(utente);
            return true;
        }

        public async Task<bool> UpdatePuntiEcoAsync(string utenteId, int nuoviPunti)
        {
            var utente = await GetByIdAsync(utenteId);
            if (utente == null) return false;

            utente.PuntiEco = nuoviPunti;
            await UpdateAsync(utente);
            return true;
        }
    }
}