using Microsoft.Extensions.Logging;
using MobiShare.Core.Enums;

namespace MobiShare.IoT.Services
{
    public class EmulatoreHue
    {
        private readonly ILogger<EmulatoreHue> _logger;
        private readonly Dictionary<string, StatoLampadaHue> _lampade = new();

        public EmulatoreHue(ILogger<EmulatoreHue> logger)
        {
            _logger = logger;
        }

        public void CreaLampada(string slotId, string nomeLampada)
        {
            var lampada = new StatoLampadaHue
            {
                Id = slotId,
                Nome = nomeLampada,
                Accesa = true,
                Colore = ColoreLuce.Verde,
                UltimoAggiornamento = DateTime.UtcNow
            };

            _lampade[slotId] = lampada;
            _logger.LogInformation("Creata lampada Hue emulata: {SlotId} - {Nome}", slotId, nomeLampada);
        }

        public void AggiornaLedSlot(string slotId, ColoreLuce colore)
        {
            if (_lampade.TryGetValue(slotId, out var lampada))
            {
                lampada.Colore = colore;
                lampada.UltimoAggiornamento = DateTime.UtcNow;

                var descrizione = OttieniDescrizioneColore(colore);
                _logger.LogInformation("💡 Slot {SlotId} LED aggiornato: {Colore} ({Descrizione})", 
                    slotId, colore, descrizione);

                // In un'implementazione reale, qui faresti la chiamata all'API Philips Hue
                // await hueApiClient.UpdateLightAsync(lampada.Id, colore);
            }
            else
            {
                _logger.LogWarning("Lampada per slot {SlotId} non trovata", slotId);
            }
        }

        public IEnumerable<StatoLampadaHue> OttieniTutteLeLampade()
        {
            return _lampade.Values;
        }

        public StatoLampadaHue? OttieniLampada(string slotId)
        {
            return _lampade.TryGetValue(slotId, out var lampada) ? lampada : null;
        }

        private string OttieniDescrizioneColore(ColoreLuce colore)
        {
            return colore switch
            {
                ColoreLuce.Verde => "Disponibile",
                ColoreLuce.Rosso => "Occupato/Non disponibile",
                ColoreLuce.Giallo => "Manutenzione",
                _ => "Sconosciuto"
            };
        }
    }

    public class StatoLampadaHue
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public bool Accesa { get; set; } = true;
        public ColoreLuce Colore { get; set; } = ColoreLuce.Verde;
        public DateTime UltimoAggiornamento { get; set; } = DateTime.UtcNow;
    }
}
