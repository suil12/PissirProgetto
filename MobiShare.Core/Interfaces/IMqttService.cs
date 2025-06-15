using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    /// <summary>
    /// Interfaccia semplificata per inviare comandi via gateway IoT
    /// Il gateway gestisce la comunicazione MQTT effettiva
    /// </summary>
    public interface IMqttService
    {
        Task<bool> PubblicaComandoMezzoAsync(string mezzoId, string comando, object? parametri = null);
        Task<bool> PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore);
        Task<bool> PubblicaNotificaSistemaAsync(string messaggio, string? mezzoId = null);
        Task<bool> IsConnectedAsync();

        // Metodi aggiuntivi per IoT Controller
        Task<bool> PubblicaEventoBatteriaScaricaAsync(string mezzoId, int percentualeBatteria);
        Task<bool> PubblicaStatoBatteriaAsync(string mezzoId, int percentualeBatteria, bool isCharging);
        Task<bool> PubblicaStatoMezzoAsync(string mezzoId, StatoMezzo stato);
        Task<bool> PubblicaStatoSlotAsync(string slotId, StatoSlot stato, string? mezzoId);
        Task<bool> PubblicaAggiornamentoLedSlotAsync(string slotId, ColoreLuce colore);
        Task<bool> PubblicaNotificaSistemaAsync(string messaggio, object? datiAggiuntivi);
        Task<bool> PubblicaEventoInizioCorsaAsync(string corsaId, string mezzoId, string utenteId);
        Task<bool> PubblicaEventoFineCorsaAsync(string corsaId, string mezzoId, TimeSpan durata, decimal costo);
    }
}