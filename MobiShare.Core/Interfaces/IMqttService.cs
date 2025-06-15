using MobiShare.Core.Models;
using MobiShare.Core.Enums;

namespace MobiShare.Core.Interfaces
{
    public interface IMqttService
    {
        Task<bool> PublishAsync(string topic, object payload);
        Task<bool> SubscribeAsync(string topic);
        Task<bool> UnsubscribeAsync(string topic);
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }

        event EventHandler<MqttMessageReceivedEventArgs> MessageReceived;
        event EventHandler<EventArgs> Connected;
        event EventHandler<EventArgs> Disconnected;

        // Metodi specifici per MobiShare
        Task<bool> PubblicaComandoMezzoAsync(string mezzoId, string comando, object dati);
        Task<bool> PubblicaAggiornamentoSlotAsync(string slotId, string parcheggioId, ColoreLuce colore);
        Task<bool> PubblicaNotificaSistemaAsync(string messaggio, object? dati = null);

        // Eventi aggiuntivi per compatibilità IoT
        event EventHandler<AggiornamentoStatoMezzoEventArgs> StatoMezzoAggiornato;
        event EventHandler<AggiornamentoSensoreSlotEventArgs> SensoreSlotAggiornato;
    }
}