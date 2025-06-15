using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface ISlotService
    {
        Task<Slot?> GetByIdAsync(string slotId);
        Task<IEnumerable<Slot>> GetByParcheggioAsync(string parcheggioId);
        Task<IEnumerable<Slot>> GetByStatoAsync(StatoSlot stato);
        Task<Slot?> GetSlotDisponibileInParcheggioAsync(string parcheggioId);
        Task<bool> UpdateStatoAsync(string slotId, StatoSlot stato);
        Task<bool> UpdateColoreLuceAsync(string slotId, ColoreLuce colore);
        Task<bool> AssegnaMezzoAsync(string slotId, string mezzoId);
        Task<bool> LiberaSlotAsync(string slotId);
    }
}
