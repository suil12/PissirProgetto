using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IMezzoService
    {
        Task<IEnumerable<Mezzo>> GetMezziDisponibiliAsync();
        Task<IEnumerable<Mezzo>> GetMezziByParcheggioAsync(string parcheggioId);
        Task<Mezzo?> GetMezzoByIdAsync(string id);
        Task<Mezzo?> GetByIdAsync(string id); // Alias per compatibilità
        Task<bool> SbloccaMezzoAsync(string mezzoId, string utenteId);
        Task<bool> BloccaMezzoAsync(string mezzoId);
        Task<bool> AggiornaBatteriaAsync(string mezzoId, int percentualeBatteria);
        Task<bool> AggiornaPosizioneAsync(string mezzoId, double latitudine, double longitudine);
        Task<bool> ImpostaStatoManutenzioneAsync(string mezzoId, bool inManutenzione);

        // Metodi aggiuntivi per compatibilità IoT
        Task<bool> UpdateBatteriaMezzoAsync(string mezzoId, int percentualeBatteria);
        Task<bool> UpdateStatoMezzoAsync(string mezzoId, StatoMezzo stato);

        // Metodi per API Controller
        Task<IEnumerable<Mezzo>> GetMezziDisponibiliVicinoAsync(double lat, double lng, double raggioKm = 1.0);
        Task<IEnumerable<Mezzo>> GetTuttiMezziAsync();
        Task<Mezzo> CreaMezzoAsync(TipoMezzo tipo, string modello, decimal tariffa, string parcheggioId);
        Task<bool> EliminaMezzoAsync(string mezzoId);
    }
}