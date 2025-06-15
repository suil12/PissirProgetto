using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface ISlotRepository : IRepository<Slot>
    {
        Task<IEnumerable<Slot>> GetByParcheggioAsync(string parcheggioId);
        Task<IEnumerable<Slot>> GetByStatoAsync(StatoSlot stato);
        Task<Slot?> GetSlotDisponibileInParcheggioAsync(string parcheggioId);
        Task<bool> UpdateStatoAsync(string slotId, StatoSlot stato);
        Task<bool> UpdateColoreLuceAsync(string slotId, ColoreLuce colore);
    }
}