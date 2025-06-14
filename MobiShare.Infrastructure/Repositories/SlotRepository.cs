using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Data;

namespace MobiShare.Infrastructure.Repositories
{
    public class SlotRepository : Repository<Slot>, ISlotRepository
    {
        public SlotRepository(MobiShareDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Slot>> GetByParcheggioAsync(string parcheggioId)
        {
            return await _dbSet
                .Include(s => s.SensoreLuce)
                .Include(s => s.MezzoPresente)
                .Where(s => s.ParcheggiId == parcheggioId)
                .OrderBy(s => s.Numero)
                .ToListAsync();
        }

        public async Task<IEnumerable<Slot>> GetByStatoAsync(StatoSlot stato)
        {
            return await _dbSet
                .Include(s => s.Parcheggio)
                .Where(s => s.Stato == stato)
                .ToListAsync();
        }

        public async Task<Slot?> GetSlotDisponibileInParcheggioAsync(string parcheggioId)
        {
            return await _dbSet
                .Include(s => s.SensoreLuce)
                .FirstOrDefaultAsync(s => s.ParcheggiId == parcheggioId && s.Stato == StatoSlot.Libero);
        }

        public async Task<bool> UpdateStatoAsync(string slotId, StatoSlot stato)
        {
            var slot = await GetByIdAsync(slotId);
            if (slot == null) return false;

            slot.Stato = stato;
            slot.UltimoAggiornamento = DateTime.UtcNow;
            await UpdateAsync(slot);
            return true;
        }
    }
}