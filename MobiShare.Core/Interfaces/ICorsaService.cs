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

        // Metodi aggiuntivi per controller
        Task<IEnumerable<Corsa>> GetCorseAsync();
        Task<IEnumerable<Corsa>> GetCorseAsync(string? utenteId, string? mezzoId, StatoCorsa? stato, DateTime? dataInizio, DateTime? dataFine);
        Task<Corsa?> GetByIdAsync(string corsaId);
        Task<Corsa?> GetCorsaInCorsoByUtenteAsync(string utenteId); // Alias per GetCorsaAttivaAsync
        Task<Corsa?> AnnullaCorsa(string corsaId);
        Task<Corsa?> AnnullaCorsa(string corsaId, string motivo); // Overload con motivo
        Task<IEnumerable<Corsa>> GetCorseAtttiveAsync();
        Task<object> GetStatisticheUtenteAsync(string utenteId);

        // Metodi wrapper per compatibilità
        Task<Corsa?> IniziaCorsa(string utenteId, string mezzoId);
        Task<Corsa?> TerminaCorsa(string corsaId);
        Task<Corsa?> TerminaCorsa(string corsaId, string parcheggioDestinazioneId); // Overload con parcheggio
        Task<IEnumerable<Corsa>> GetStoricoCorse(string utenteId);
        decimal CalcolaCosto(TimeSpan durata, TipoMezzo tipoMezzo);
        Task<bool> VerificaDisponibilitaMezzo(string mezzoId);
    }
}