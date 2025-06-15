using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface ICorsaService
    {
        Task<Corsa?> IniziaCorsaAsync(string utenteId, string mezzoId);
        Task<Corsa?> TerminaCorsaAsync(string corsaId, string parcheggioDestinazioneId);
        Task<Corsa?> GetCorsaAttivaAsync(string utenteId);
        Task<IEnumerable<Corsa>> GetStoricoCorseAsync(string utenteId);
        Task<decimal> CalcolaCostoCorsaAsync(string corsaId);
        Task<int> CalcolaPuntiEcoAsync(string corsaId);

        // Metodi wrapper per compatibilità
        Task<Corsa?> IniziaCorsa(string utenteId, string mezzoId);
        Task<Corsa?> TerminaCorsa(string corsaId);
        Task<IEnumerable<Corsa>> GetStoricoCorse(string utenteId);
        decimal CalcolaCosto(TimeSpan durata, TipoMezzo tipoMezzo);
        Task<bool> VerificaDisponibilitaMezzo(string mezzoId);
    }
}