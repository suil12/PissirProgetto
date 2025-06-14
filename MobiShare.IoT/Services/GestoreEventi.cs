using Microsoft.Extensions.Logging;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Enums;

namespace MobiShare.IoT.Services
{
    public class GestoreEventiIoT
    {
        private readonly ILogger<GestoreEventiIoT> _logger;
        private readonly IMezzoService _servizioMezzi;
        private readonly IParcheggioService _servizioParcheggi;
        private readonly EmulatoreHue _emulatoreHue;

        public GestoreEventiIoT(
            ILogger<GestoreEventiIoT> logger,
            IMezzoService servizioMezzi,
            IParcheggioService servizioParcheggi,
            EmulatoreHue emulatoreHue)
        {
            _logger = logger;
            _servizioMezzi = servizioMezzi;
            _servizioParcheggi = servizioParcheggi;
            _emulatoreHue = emulatoreHue;
        }

        public async Task GestisciAggiornamentoStatoMezzo(AggiornamentoStatoMezzoEventArgs args)
        {
            try
            {
                // Aggiorna stato mezzo nel database
                if (args.PercentualeBatteria.HasValue)
                {
                    await _servizioMezzi.UpdateBatteriaMezzoAsync(args.MezzoId, args.PercentualeBatteria.Value);
                }

                await _servizioMezzi.UpdateStatoMezzoAsync(args.MezzoId, args.Stato);

                _logger.LogInformation("Aggiornato stato mezzo {MezzoId}: {Stato}", args.MezzoId, args.Stato);

                // Gestione automatica LED slot in base allo stato
                await AggiornaLedSlotInBaseAlStatoMezzo(args.MezzoId, args.Stato);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento stato mezzo {MezzoId}", args.MezzoId);
            }
        }

        public async Task GestisciAggiornamentoSensoreSlot(AggiornamentoSensoreSlotEventArgs args)
        {
            try
            {
                // Aggiorna stato slot nel database
                await _servizioParcheggi.UpdateStatoSlotAsync(args.SlotId, args.Stato);

                // Aggiorna LED emulato
                _emulatoreHue.AggiornaLedSlot(args.SlotId, args.ColoreLuce);

                _logger.LogInformation("Aggiornato sensore slot {SlotId}: {Stato} - {Colore}", 
                    args.SlotId, args.Stato, args.ColoreLuce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento sensore slot {SlotId}", args.SlotId);
            }
        }

        private async Task AggiornaLedSlotInBaseAlStatoMezzo(string mezzoId, StatoMezzo stato)
        {
            try
            {
                var mezzo = await _servizioMezzi.GetMezzoByIdAsync(mezzoId);
                if (mezzo?.SlotId == null) return;

                ColoreLuce nuovoColore = stato switch
                {
                    StatoMezzo.Disponibile => ColoreLuce.Verde,
                    StatoMezzo.InUso => ColoreLuce.Rosso,
                    StatoMezzo.Manutenzione or StatoMezzo.BatteriaScarica => ColoreLuce.Giallo,
                    _ => ColoreLuce.Verde
                };

                _emulatoreHue.AggiornaLedSlot(mezzo.SlotId, nuovoColore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento LED per mezzo {MezzoId}", mezzoId);
            }
        }
    }
}