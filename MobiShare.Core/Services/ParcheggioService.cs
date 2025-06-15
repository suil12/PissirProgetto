using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class ParcheggioService : IParcheggioService
    {
        private readonly IParcheggioRepository _parcheggioRepository;
        private readonly ISlotRepository _slotRepository;

        public ParcheggioService(IParcheggioRepository parcheggioRepository, ISlotRepository slotRepository)
        {
            _parcheggioRepository = parcheggioRepository;
            _slotRepository = slotRepository;
        }

        public async Task<IEnumerable<Parcheggio>> GetTuttiParcheggiAsync()
        {
            return await _parcheggioRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Parcheggio>> GetParcheggiViciniAsync(double lat, double lng, double raggioKm = 2.0)
        {
            return await _parcheggioRepository.GetVicinoAsync(lat, lng, raggioKm);
        }

        public async Task<Parcheggio?> GetParcheggioConDettagliAsync(string parcheggioId)
        {
            return await _parcheggioRepository.GetConSlotsAsync(parcheggioId);
        }

        public async Task<Parcheggio> CreaParcheggioAsync(string nome, double lat, double lng, int capacita)
        {
            var parcheggio = new Parcheggio
            {
                Nome = nome,
                Latitudine = lat,
                Longitudine = lng,
                Capacita = capacita
            };

            var parcheggioPcreato = await _parcheggioRepository.AddAsync(parcheggio);

            // Crea slots per il parcheggio
            for (int i = 1; i <= capacita; i++)
            {
                var slot = new Slot
                {
                    Numero = i,
                    ParcheggiId = parcheggioPcreato.Id,
                    Stato = StatoSlot.Libero
                };

                await _slotRepository.AddAsync(slot);
            }

            return parcheggioPcreato;
        }

        public async Task<Slot?> GetSlotDisponibileAsync(string parcheggioId)
        {
            return await _slotRepository.GetSlotDisponibileInParcheggioAsync(parcheggioId);
        }

        public async Task<bool> UpdateStatoSlotAsync(string slotId, StatoSlot stato)
        {
            return await _slotRepository.UpdateStatoAsync(slotId, stato);
        }

        // Implementazione dei metodi richiesti dall'interfaccia IParcheggioService
        public async Task<Parcheggio?> GetParcheggioByIdAsync(string id)
        {
            return await _parcheggioRepository.GetByIdAsync(id);
        }

        public async Task<Parcheggio?> GetByIdAsync(string id)
        {
            return await GetParcheggioByIdAsync(id);
        }

        public async Task<IEnumerable<Slot>> GetSlotsByParcheggioAsync(string parcheggioId)
        {
            return await _slotRepository.GetByParcheggioAsync(parcheggioId);
        }

        public async Task<bool> AggiornaStatoSlotAsync(string slotId, StatoSlot stato)
        {
            return await UpdateStatoSlotAsync(slotId, stato);
        }

        public async Task<bool> AggiornaColoreLuceSlotAsync(string slotId, ColoreLuce colore)
        {
            return await _slotRepository.UpdateColoreLuceAsync(slotId, colore);
        }

        public async Task<int> GetPostiLiberiAsync(string parcheggioId)
        {
            var slots = await _slotRepository.GetByParcheggioAsync(parcheggioId);
            return slots.Count(s => s.Stato == StatoSlot.Libero);
        }

        public async Task<int> GetPostiOccupatiAsync(string parcheggioId)
        {
            var slots = await _slotRepository.GetByParcheggioAsync(parcheggioId);
            return slots.Count(s => s.Stato == StatoSlot.Occupato);
        }

        public async Task<Parcheggio> CreaParcheggioAsync(string nome, string indirizzo, double lat, double lng, int numeroSlots)
        {
            var parcheggio = new Parcheggio
            {
                Nome = nome,
                Indirizzo = indirizzo,
                Latitudine = lat,
                Longitudine = lng,
                Capacita = numeroSlots
            };

            var parcheggioCreato = await _parcheggioRepository.AddAsync(parcheggio);

            // Crea gli slot per il parcheggio
            for (int i = 1; i <= numeroSlots; i++)
            {
                var slot = new Slot
                {
                    Numero = i,
                    ParcheggiId = parcheggioCreato.Id,
                    Stato = StatoSlot.Libero,
                    SensoreLuce = new DatiSensore
                    {
                        Colore = ColoreLuce.Verde,
                        StatoAttivo = true
                    }
                };
                await _slotRepository.AddAsync(slot);
            }

            return parcheggioCreato;
        }
    }
}