using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IMezzoRepository : IRepository<Mezzo>
    {
        Task<IEnumerable<Mezzo>> GetDisponibiliVicinoAsync(double lat, double lng, double raggioKm);
        Task<IEnumerable<Mezzo>> GetByTipoAsync(TipoMezzo tipo);
        Task<IEnumerable<Mezzo>> GetByStatoAsync(StatoMezzo stato);
        Task<IEnumerable<Mezzo>> GetByParcheggioAsync(string parcheggioId);
        Task<bool> UpdateStatoAsync(string mezzoId, StatoMezzo stato);
        Task<bool> UpdateBatteriaAsync(string mezzoId, int percentualeBatteria);
        Task<bool> UpdatePosizioneAsync(string mezzoId, double lat, double lng);
    }
}