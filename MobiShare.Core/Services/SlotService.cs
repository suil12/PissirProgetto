using Microsoft.Extensions.Logging;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class SlotService : ISlotService
    {
        private readonly ISlotRepository _slotRepository;
        private readonly ILogger<SlotService> _logger;

        public SlotService(ISlotRepository slotRepository, ILogger<SlotService> logger)
        {
            _slotRepository = slotRepository;
            _logger = logger;
        }

        public async Task<Slot?> GetByIdAsync(string slotId)
        {
            try
            {
                return await _slotRepository.GetByIdAsync(slotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dello slot {SlotId}", slotId);
                return null;
            }
        }

        public async Task<IEnumerable<Slot>> GetByParcheggioAsync(string parcheggioId)
        {
            try
            {
                return await _slotRepository.GetByParcheggioAsync(parcheggioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero degli slot per parcheggio {ParcheggioId}", parcheggioId);
                return Enumerable.Empty<Slot>();
            }
        }

        public async Task<IEnumerable<Slot>> GetByStatoAsync(StatoSlot stato)
        {
            try
            {
                return await _slotRepository.GetByStatoAsync(stato);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero degli slot per stato {Stato}", stato);
                return Enumerable.Empty<Slot>();
            }
        }

        public async Task<Slot?> GetSlotDisponibileInParcheggioAsync(string parcheggioId)
        {
            try
            {
                return await _slotRepository.GetSlotDisponibileInParcheggioAsync(parcheggioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero di slot disponibile per parcheggio {ParcheggioId}", parcheggioId);
                return null;
            }
        }

        public async Task<bool> UpdateStatoAsync(string slotId, StatoSlot stato)
        {
            try
            {
                var result = await _slotRepository.UpdateStatoAsync(slotId, stato);
                if (result)
                {
                    _logger.LogInformation("Stato slot {SlotId} aggiornato a {Stato}", slotId, stato);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento stato slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> UpdateColoreLuceAsync(string slotId, ColoreLuce colore)
        {
            try
            {
                var result = await _slotRepository.UpdateColoreLuceAsync(slotId, colore);
                if (result)
                {
                    _logger.LogInformation("Colore luce slot {SlotId} aggiornato a {Colore}", slotId, colore);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento colore luce slot {SlotId}", slotId);
                return false;
            }
        }

        public async Task<bool> AssegnaMezzoAsync(string slotId, string mezzoId)
        {
            try
            {
                var slot = await _slotRepository.GetByIdAsync(slotId);
                if (slot == null)
                {
                    _logger.LogWarning("Slot {SlotId} non trovato per assegnazione mezzo", slotId);
                    return false;
                }

                slot.MezzoId = mezzoId;
                slot.Stato = StatoSlot.Occupato;

                await _slotRepository.UpdateAsync(slot);
                _logger.LogInformation("Mezzo {MezzoId} assegnato allo slot {SlotId}", mezzoId, slotId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'assegnazione mezzo {MezzoId} allo slot {SlotId}", mezzoId, slotId);
                return false;
            }
        }

        public async Task<bool> LiberaSlotAsync(string slotId)
        {
            try
            {
                var slot = await _slotRepository.GetByIdAsync(slotId);
                if (slot == null)
                {
                    _logger.LogWarning("Slot {SlotId} non trovato per liberazione", slotId);
                    return false;
                }

                slot.MezzoId = null;
                slot.Stato = StatoSlot.Libero;

                await _slotRepository.UpdateAsync(slot);
                _logger.LogInformation("Slot {SlotId} liberato", slotId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella liberazione dello slot {SlotId}", slotId);
                return false;
            }
        }
    }
}
