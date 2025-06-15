using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class CorsaService : ICorsaService
    {
        private readonly ICorsaRepository _corsaRepository;
        private readonly IMezzoRepository _mezzoRepository;
        private readonly IUtenteRepository _utenteRepository;
        private readonly IParcheggioRepository _parcheggioRepository;
        private readonly ISlotRepository _slotRepository;
        private readonly IPuntiEcoService _puntiEcoService;
        private readonly IMqttService _mqttService;

        public CorsaService(
            ICorsaRepository corsaRepository,
            IMezzoRepository mezzoRepository,
            IUtenteRepository utenteRepository,
            IParcheggioRepository parcheggioRepository,
            ISlotRepository slotRepository,
            IPuntiEcoService puntiEcoService,
            IMqttService mqttService)
        {
            _corsaRepository = corsaRepository;
            _mezzoRepository = mezzoRepository;
            _utenteRepository = utenteRepository;
            _parcheggioRepository = parcheggioRepository;
            _slotRepository = slotRepository;
            _puntiEcoService = puntiEcoService;
            _mqttService = mqttService;
        }

        public async Task<Corsa?> IniziaCorsaAsync(string utenteId, string mezzoId)
        {
            // Verifica se l'utente ha già una corsa attiva
            var corsaAttiva = await _corsaRepository.GetCorsaAttivaByUtenteAsync(utenteId);
            if (corsaAttiva != null)
                return null;

            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);

            if (utente == null || mezzo == null || mezzo.Stato != StatoMezzo.Disponibile)
                return null;

            // Verifica credito minimo (almeno 2€)
            if (utente.Credito < 2.00m)
                return null;

            // Crea la corsa
            var corsa = new Corsa
            {
                UtenteId = utenteId,
                MezzoId = mezzoId,
                ParcheggioDiPartenzaId = mezzo.ParcheggioDiPartenzaId!,
                DataInizio = DateTime.UtcNow,
                Stato = StatoCorsa.InCorso
            };

            // Aggiorna stato mezzo
            await _mezzoRepository.UpdateStatoAsync(mezzoId, StatoMezzo.InUso);

            // Libera slot se il mezzo era in un parcheggio
            if (!string.IsNullOrEmpty(mezzo.SlotId))
            {
                await _slotRepository.UpdateStatoAsync(mezzo.SlotId, StatoSlot.Libero);
                // Aggiorna LED slot a verde
                await _mqttService.PubblicaAggiornamentoSlotAsync(mezzo.SlotId, mezzo.ParcheggioDiPartenzaId!, ColoreLuce.Verde);
            }

            // Invia comando sblocco via MQTT
            await _mqttService.PubblicaComandoMezzoAsync(mezzoId, "SBLOCCA", new { CorsaId = corsa.Id, UtenteId = utenteId });

            return await _corsaRepository.AddAsync(corsa);
        }

        public async Task<Corsa?> TerminaCorsaAsync(string corsaId, string parcheggioDestinazioneId)
        {
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null || corsa.Stato != StatoCorsa.InCorso)
                return null;

            var mezzo = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            var utente = await _utenteRepository.GetByIdAsync(corsa.UtenteId);
            var parcheggioDestinazione = await _parcheggioRepository.GetByIdAsync(parcheggioDestinazioneId);

            if (mezzo == null || utente == null || parcheggioDestinazione == null)
                return null;

            // Trova uno slot disponibile nel parcheggio di destinazione
            var slotDisponibile = await _slotRepository.GetSlotDisponibileInParcheggioAsync(parcheggioDestinazioneId);
            if (slotDisponibile == null)
                return null; // Nessuno slot disponibile

            // Aggiorna corsa
            corsa.DataFine = DateTime.UtcNow;
            corsa.ParcheggioDestinazioneId = parcheggioDestinazioneId;
            corsa.Stato = StatoCorsa.Completata;

            // Calcola costo e punti eco
            corsa.Costo = await CalcolaCostoCorsaAsync(corsaId);
            corsa.PuntiEcoAccumulati = await CalcolaPuntiEcoAsync(corsaId);

            // Aggiorna credito utente
            await _utenteRepository.UpdateCreditoAsync(utente.Id, utente.Credito - corsa.Costo);

            // Aggiorna punti eco se bici muscolare
            if (corsa.PuntiEcoAccumulati > 0)
            {
                await _utenteRepository.UpdatePuntiEcoAsync(utente.Id, utente.PuntiEco + corsa.PuntiEcoAccumulati);
            }

            // Aggiorna mezzo
            await _mezzoRepository.UpdateStatoAsync(mezzo.Id, StatoMezzo.Disponibile);
            await _mezzoRepository.UpdatePosizioneAsync(mezzo.Id, parcheggioDestinazione.Latitudine, parcheggioDestinazione.Longitudine);

            // Occupa slot
            slotDisponibile.Stato = StatoSlot.Occupato;
            slotDisponibile.MezzoId = mezzo.Id;
            await _slotRepository.UpdateAsync(slotDisponibile);

            // Aggiorna LED slot a rosso (occupato)
            await _mqttService.PubblicaAggiornamentoSlotAsync(slotDisponibile.Id, parcheggioDestinazioneId, ColoreLuce.Rosso);

            // Invia comando blocco via MQTT
            await _mqttService.PubblicaComandoMezzoAsync(mezzo.Id, "BLOCCA", new { CorsaId = corsaId });

            await _corsaRepository.UpdateAsync(corsa);
            return corsa;
        }

        public async Task<Corsa?> GetCorsaAttivaAsync(string utenteId)
        {
            return await _corsaRepository.GetCorsaAttivaByUtenteAsync(utenteId);
        }

        public async Task<IEnumerable<Corsa>> GetStoricoCorseAsync(string utenteId)
        {
            return await _corsaRepository.GetByUtenteAsync(utenteId);
        }

        public async Task<decimal> CalcolaCostoCorsaAsync(string corsaId)
        {
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null || !corsa.DataFine.HasValue)
                return 0;

            var mezzo = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            if (mezzo == null)
                return 0;

            var durataMinuti = (corsa.DataFine.Value - corsa.DataInizio).TotalMinutes;
            return (decimal)durataMinuti * mezzo.TariffaPerMinuto;
        }

        public async Task<int> CalcolaPuntiEcoAsync(string corsaId)
        {
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null)
                return 0;

            return _puntiEcoService.CalcolaPuntiEco(corsa);
        }

        // Metodi wrapper per implementare l'interfaccia ICorsaService
        public async Task<Corsa?> IniziaCorsa(string utenteId, string mezzoId)
        {
            return await IniziaCorsaAsync(utenteId, mezzoId);
        }

        public async Task<Corsa?> TerminaCorsa(string corsaId)
        {
            // Per terminare una corsa, dobbiamo trovare un parcheggio disponibile
            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null) return null;

            // Trova il parcheggio più vicino o usa quello di partenza
            return await TerminaCorsaAsync(corsaId, corsa.ParcheggioDiPartenzaId);
        }

        public async Task<IEnumerable<Corsa>> GetStoricoCorse(string utenteId)
        {
            return await GetStoricoCorseAsync(utenteId);
        }

        public decimal CalcolaCosto(TimeSpan durata, TipoMezzo tipoMezzo)
        {
            // Calcolo base del costo in base al tipo di mezzo
            var tariffaPerMinuto = tipoMezzo switch
            {
                TipoMezzo.BiciMuscolare => 0.05m,
                TipoMezzo.BiciElettrica => 0.10m,
                TipoMezzo.Monopattino => 0.15m,
                _ => 0.10m
            };

            return (decimal)durata.TotalMinutes * tariffaPerMinuto;
        }

        public async Task<bool> VerificaDisponibilitaMezzo(string mezzoId)
        {
            var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);
            return mezzo != null && mezzo.Stato == StatoMezzo.Disponibile;
        }
    }
}