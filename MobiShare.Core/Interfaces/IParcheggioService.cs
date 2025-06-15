using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IParcheggioService
    {
        Task<IEnumerable<Parcheggio>> GetTuttiParcheggiAsync();
        Task<Parcheggio?> GetParcheggioByIdAsync(string id);
        Task<Parcheggio?> GetByIdAsync(string id); // Alias per compatibilità
        Task<IEnumerable<Slot>> GetSlotsByParcheggioAsync(string parcheggioId);
        Task<bool> AggiornaStatoSlotAsync(string slotId, StatoSlot nuovoStato);
        Task<bool> AggiornaColoreLuceSlotAsync(string slotId, ColoreLuce colore);
        Task<int> GetPostiLiberiAsync(string parcheggioId);
        Task<int> GetPostiOccupatiAsync(string parcheggioId);

        // Metodi aggiuntivi per API Controller
        Task<IEnumerable<Parcheggio>> GetParcheggiViciniAsync(double lat, double lng, double raggioKm = 2.0);
        Task<Parcheggio?> GetParcheggioConDettagliAsync(string parcheggioId);
        Task<Parcheggio> CreaParcheggioAsync(string nome, string indirizzo, double lat, double lng, int numeroSlots);

        // Metodo aggiuntivo per compatibilità IoT
        Task<bool> UpdateStatoSlotAsync(string slotId, StatoSlot stato);
    }
}