using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Data;

namespace MobiShare.Infrastructure.Repositories
{
    public class ParcheggioRepository : Repository<Parcheggio>, IParcheggioRepository
    {
        public ParcheggioRepository(MobiShareDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Parcheggio>> GetVicinoAsync(double lat, double lng, double raggioKm)
        {
            var parcheggi = await _dbSet.ToListAsync();
            return parcheggi.Where(p => CalcolaDistanza(lat, lng, p.Latitudine, p.Longitudine) <= raggioKm);
        }

        public async Task<Parcheggio?> GetConSlotsAsync(string parcheggioId)
        {
            return await _dbSet
                .Include(p => p.Slots)
                .ThenInclude(s => s.SensoreLuce)
                .Include(p => p.MezziPresenti)
                .FirstOrDefaultAsync(p => p.Id == parcheggioId);
        }

        public async Task<int> GetConteggiSlotsDisponibiliAsync(string parcheggioId)
        {
            var parcheggio = await _dbSet
                .Include(p => p.Slots)
                .FirstOrDefaultAsync(p => p.Id == parcheggioId);

            return parcheggio?.Slots.Count(s => s.Stato == StatoSlot.Libero) ?? 0;
        }

        public async Task<IEnumerable<Mezzo>> GetMezziInParcheggioAsync(string parcheggioId)
        {
            return await _context.Mezzi
                .Where(m => m.ParcheggioDiPartenzaId == parcheggioId)
                .ToListAsync();
        }

        private static double CalcolaDistanza(double lat1, double lng1, double lat2, double lng2)
        {
            var R = 6371; // Raggio della Terra in chilometri
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}