using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.Infrastructure.Data;

namespace MobiShare.Infrastructure.Repositories
{
    public class MezzoRepository : Repository<Mezzo>, IMezzoRepository
    {
        public MezzoRepository(MobiShareDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Mezzo>> GetDisponibiliVicinoAsync(double lat, double lng, double raggioKm)
        {
            var mezzi = await _dbSet
                .Include(m => m.ParcheggioDiPartenza)
                .Where(m => m.Stato == StatoMezzo.Disponibile)
                .ToListAsync();

            return mezzi.Where(m => CalcolaDistanza(lat, lng, m.Latitudine, m.Longitudine) <= raggioKm);
        }

        public async Task<IEnumerable<Mezzo>> GetByTipoAsync(TipoMezzo tipo)
        {
            return await _dbSet.Where(m => m.Tipo == tipo).ToListAsync();
        }

        public async Task<IEnumerable<Mezzo>> GetByStatoAsync(StatoMezzo stato)
        {
            return await _dbSet.Where(m => m.Stato == stato).ToListAsync();
        }

        public async Task<IEnumerable<Mezzo>> GetByParcheggioAsync(string parcheggioId)
        {
            return await _dbSet.Where(m => m.ParcheggioDiPartenzaId == parcheggioId).ToListAsync();
        }

        public async Task<bool> UpdateStatoAsync(string mezzoId, StatoMezzo stato)
        {
            var mezzo = await GetByIdAsync(mezzoId);
            if (mezzo == null) return false;

            mezzo.Stato = stato;
            await UpdateAsync(mezzo);
            return true;
        }

        public async Task<bool> UpdateBatteriaAsync(string mezzoId, int percentualeBatteria)
        {
            var mezzo = await GetByIdAsync(mezzoId);
            if (mezzo == null) return false;

            mezzo.PercentualeBatteria = percentualeBatteria;
            await UpdateAsync(mezzo);
            return true;
        }

        public async Task<bool> UpdatePosizioneAsync(string mezzoId, double lat, double lng)
        {
            var mezzo = await GetByIdAsync(mezzoId);
            if (mezzo == null) return false;

            mezzo.Latitudine = lat;
            mezzo.Longitudine = lng;
            await UpdateAsync(mezzo);
            return true;
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