using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class MezzoService : IMezzoService
    {
        private readonly IMezzoRepository _mezzoRepository;
        private readonly IParcheggioRepository _parcheggioRepository;

        public MezzoService(IMezzoRepository mezzoRepository, IParcheggioRepository parcheggioRepository)
        {
            _mezzoRepository = mezzoRepository;
            _parcheggioRepository = parcheggioRepository;
        }

        public async Task<IEnumerable<Mezzo>> GetMezziDisponibiliVicinoAsync(double lat, double lng, double raggioKm = 1.0)
        {
            return await _mezzoRepository.GetDisponibiliVicinoAsync(lat, lng, raggioKm);
        }

        public async Task<IEnumerable<Mezzo>> GetTuttiMezziAsync()
        {
            return await _mezzoRepository.GetAllAsync();
        }

        public async Task<Mezzo?> GetMezzoByIdAsync(string mezzoId)
        {
            return await _mezzoRepository.GetByIdAsync(mezzoId);
        }

        public async Task<bool> UpdateStatoMezzoAsync(string mezzoId, StatoMezzo stato)
        {
            return await _mezzoRepository.UpdateStatoAsync(mezzoId, stato);
        }

        public async Task<bool> UpdateBatteriaMezzoAsync(string mezzoId, int percentualeBatteria)
        {
            var success = await _mezzoRepository.UpdateBatteriaAsync(mezzoId, percentualeBatteria);
            
            // Se batteria scarica, aggiorna automaticamente lo stato
            if (success && percentualeBatteria <= 20)
            {
                await UpdateStatoMezzoAsync(mezzoId, StatoMezzo.BatteriaScarica);
            }
            else if (success && percentualeBatteria > 20)
            {
                var mezzo = await GetMezzoByIdAsync(mezzoId);
                if (mezzo?.Stato == StatoMezzo.BatteriaScarica)
                {
                    await UpdateStatoMezzoAsync(mezzoId, StatoMezzo.Disponibile);
                }
            }

            return success;
        }

        public async Task<Mezzo> CreaMezzoAsync(TipoMezzo tipo, string modello, decimal tariffa, string parcheggioId)
        {
            var parcheggio = await _parcheggioRepository.GetByIdAsync(parcheggioId);
            if (parcheggio == null)
                throw new ArgumentException("Parcheggio non trovato");

            var mezzo = new Mezzo
            {
                Tipo = tipo,
                Modello = modello,
                TariffaPerMinuto = tariffa,
                ParcheggioDiPartenzaId = parcheggioId,
                Latitudine = parcheggio.Latitudine,
                Longitudine = parcheggio.Longitudine,
                PercentualeBatteria = tipo != TipoMezzo.BiciMuscolare ? 100 : null
            };

            return await _mezzoRepository.AddAsync(mezzo);
        }

        public async Task<bool> EliminaMezzoAsync(string mezzoId)
        {
            var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);
            if (mezzo == null || mezzo.Stato == StatoMezzo.InUso)
                return false;

            await _mezzoRepository.DeleteAsync(mezzoId);
            return true;
        }

        public async Task<IEnumerable<Mezzo>> GetMezziByTipoAsync(TipoMezzo tipo)
        {
            return await _mezzoRepository.GetByTipoAsync(tipo);
        }
    }
}