using MobiShare.Core.Entities;

namespace MobiShare.Core.Interfaces
{
    public interface IParcheggioRepository : IRepository<Parcheggio>
    {
        Task<IEnumerable<Parcheggio>> GetVicinoAsync(double lat, double lng, double raggioKm);
        Task<Parcheggio?> GetConSlotsAsync(string parcheggioId);
        Task<int> GetConteggiSlotsDisponibiliAsync(string parcheggioId);
        Task<IEnumerable<Mezzo>> GetMezziInParcheggioAsync(string parcheggioId);
    }
}