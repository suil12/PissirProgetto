using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IMqttService
    {
        Task<bool> PubblicaComandoMezzoAsync(string mezzoId, string comando, object? parametri = null);
        Task<bool> PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore);
        Task<bool> PubblicaNotificaSistemaAsync(string messaggio, string? mezzoId = null);
        Task<bool> IsConnectedAsync();
    }
}