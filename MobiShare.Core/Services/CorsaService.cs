using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;
using MobiShare.API.Services; // Per IIoTCommandService

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
        private readonly IIoTCommandService _iotCommandService; // NUOVO: sostituisce _mqttService
        private readonly ILogger<CorsaService> _logger;

        public CorsaService(
            ICorsaRepository corsaRepository,
            IMezzoRepository mezzoRepository,
            IUtenteRepository utenteRepository,
            IParcheggioRepository parcheggioRepository,
            ISlotRepository slotRepository,
            IPuntiEcoService puntiEcoService,
            IIoTCommandService iotCommandService, // NUOVO
            ILogger<CorsaService> logger)
        {
            _corsaRepository = corsaRepository;
            _mezzoRepository = mezzoRepository;
            _utenteRepository = utenteRepository;
            _parcheggioRepository = parcheggioRepository;
            _slotRepository = slotRepository;
            _puntiEcoService = puntiEcoService;
            _iotCommandService = iotCommandService; // NUOVO
            _logger = logger;
        }

        public async Task<Corsa?> IniziaCorsaAsync(string utenteId, string mezzoId)
        {
            _logger.LogInformation("Inizio corsa: Utente {UtenteId}, Mezzo {MezzoId}", utenteId, mezzoId);

            // Verifica se l'utente ha già una corsa attiva
            var corsaAttiva = await _corsaRepository.GetCorsaAttivaByUtenteAsync(utenteId);
            if (corsaAttiva != null)
            {
                _logger.LogWarning("Utente {UtenteId} ha già una corsa attiva: {CorsaId}", utenteId, corsaAttiva.Id);
                return null;
            }

            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            var mezzo = await _mezzoRepository.GetByIdAsync(mezzoId);

            if (utente == null || mezzo == null || mezzo.Stato != StatoMezzo.Disponibile)
            {
                _logger.LogWarning("Impossibile iniziare corsa: Utente={UtenteExists}, Mezzo={MezzoExists}, StatoMezzo={StatoMezzo}",
                    utente != null, mezzo != null, mezzo?.Stato);
                return null;
            }

            // Verifica credito minimo (almeno 2€)
            if (utente.Credito < 2.00m)
            {
                _logger.LogWarning("Credito insufficiente per utente {UtenteId}: {Credito}", utenteId, utente.Credito);
                return null;
            }

            // Crea la corsa
            var corsa = new Corsa
            {
                UtenteId = utenteId,
                MezzoId = mezzoId,
                ParcheggioDiPartenzaId = mezzo.ParcheggioDiPartenzaId!,
                DataInizio = DateTime.UtcNow,
                Stato = StatoCorsa.InCorso
            };

            try
            {
                // 1. COMANDO FISICO: Sblocca il mezzo tramite microservizio MQTT
                var sbloccaResult = await _iotCommandService.UnblockVehicleAsync(mezzoId);
                if (!sbloccaResult)
                {
                    _logger.LogError("Impossibile sbloccare il mezzo {MezzoId} tramite MQTT", mezzoId);
                    return null; // Se il mezzo non si sblocca fisicamente, annulla l'operazione
                }

                _logger.LogInformation("Mezzo {MezzoId} sbloccato fisicamente tramite MQTT", mezzoId);

                // 2. Aggiorna stato mezzo nel database
                await _mezzoRepository.UpdateStatoAsync(mezzoId, StatoMezzo.InUso);

                // 3. Libera slot se il mezzo era in un parcheggio
                if (!string.IsNullOrEmpty(mezzo.SlotId))
                {
                    await _slotRepository.UpdateStatoAsync(mezzo.SlotId, StatoSlot.Libero);

                    // 4. COMANDO FISICO: Cambia LED slot a verde (libero)
                    await _iotCommandService.ChangeSlotLightColorAsync(mezzo.SlotId, ColoreLuce.Verde);
                    _logger.LogInformation("LED slot {SlotId} cambiato a verde (libero)", mezzo.SlotId);
                }

                // 5. Salva la corsa nel database
                var corsaCreata = await _corsaRepository.AddAsync(corsa);

                _logger.LogInformation("Corsa {CorsaId} iniziata con successo per utente {UtenteId} e mezzo {MezzoId}",
                    corsaCreata.Id, utenteId, mezzoId);

                return corsaCreata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'inizio della corsa per utente {UtenteId} e mezzo {MezzoId}", utenteId, mezzoId);

                // In caso di errore, prova a ribloccare il mezzo
                try
                {
                    await _iotCommandService.BlockVehicleAsync(mezzoId);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Errore anche nel rollback del blocco mezzo {MezzoId}", mezzoId);
                }

                return null;
            }
        }

        public async Task<Corsa?> TerminaCorsaAsync(string corsaId, string parcheggioDestinazioneId)
        {
            _logger.LogInformation("Termine corsa: CorsaId {CorsaId}, Parcheggio destinazione {ParcheggioId}",
                corsaId, parcheggioDestinazioneId);

            var corsa = await _corsaRepository.GetByIdAsync(corsaId);
            if (corsa == null || corsa.Stato != StatoCorsa.InCorso)
            {
                _logger.LogWarning("Corsa {CorsaId} non trovata o non in corso", corsaId);
                return null;
            }

            var mezzo = await _mezzoRepository.GetByIdAsync(corsa.MezzoId);
            var utente = await _utenteRepository.GetByIdAsync(corsa.UtenteId);
            var parcheggioDestinazione = await _parcheggioRepository.GetByIdAsync(parcheggioDestinazioneId);

            if (mezzo == null || utente == null || parcheggioDestinazione == null)
            {
                _logger.LogWarning("Dati mancanti per terminare corsa {CorsaId}", corsaId);
                return null;
            }

            // Trova uno slot disponibile nel parcheggio di destinazione
            var slotDisponibile = await _slotRepository.GetSlotDisponibileInParcheggioAsync(parcheggioDestinazioneId);
            if (slotDisponibile == null)
            {
                _logger.LogWarning("Nessuno slot disponibile nel parcheggio {ParcheggioId}", parcheggioDestinazioneId);
                return null;
            }

            try
            {
                // 1. COMANDO FISICO: Blocca il mezzo tramite microservizio MQTT
                var bloccaResult = await _iotCommandService.BlockVehicleAsync(mezzo.Id);
                if (!bloccaResult)
                {
                    _logger.LogError("Impossibile bloccare il mezzo {MezzoId} tramite MQTT", mezzo.Id);
                    return null;
                }

                _logger.LogInformation("Mezzo {MezzoId} bloccato fisicamente tramite MQTT", mezzo.Id);

                // 2. Aggiorna corsa
                corsa.DataFine = DateTime.UtcNow;
                corsa.ParcheggioDestinazioneId = parcheggioDestinazioneId;
                corsa.Stato = StatoCorsa.Completata;

                // 3. Calcola costo e punti eco
                corsa.Costo = await CalcolaCostoCorsaAsync(corsaId);
                corsa.PuntiEcoAccumulati = await CalcolaPuntiEcoAsync(corsaId);

                // 4. Aggiorna credito utente
                await _utenteRepository.UpdateCreditoAsync(utente.Id, utente.Credito - corsa.Costo);

                // 5. Aggiorna punti eco se bici muscolare
                if (corsa.PuntiEcoAccumulati > 0)
                {
                    await _utenteRepository.UpdatePuntiEcoAsync(utente.Id, utente.PuntiEco + corsa.PuntiEcoAccumulati);
                }

                // 6. Aggiorna mezzo nel database
                await _mezzoRepository.UpdateStatoAsync(mezzo.Id, StatoMezzo.Disponibile);
                await _mezzoRepository.UpdatePosizioneAsync(mezzo.Id, parcheggioDestinazione.Latitudine, parcheggioDestinazione.Longitudine);

                // 7. Occupa slot nel database
                slotDisponibile.Stato = StatoSlot.Occupato;
                slotDisponibile.MezzoId = mezzo.Id;
                await _slotRepository.UpdateAsync(slotDisponibile);

                // 8. COMANDO FISICO: Cambia LED slot a rosso (occupato)
                await _iotCommandService.ChangeSlotLightColorAsync(slotDisponibile.Id, ColoreLuce.Rosso);

                _logger.LogInformation("LED slot {SlotId} cambiato a rosso (occupato)", slotDisponibile.Id);

                // 9. Salva corsa aggiornata
                await _corsaRepository.UpdateAsync(corsa);

                _logger.LogInformation("Corsa {CorsaId} terminata con successo. Costo: {Costo}€, Punti eco: {PuntiEco}",
                    corsaId, corsa.Costo, corsa.PuntiEcoAccumulati);

                return corsa;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il termine della corsa {CorsaId}", corsaId);
                return null;
            }
        }

        // Altri metodi rimangono invariati...
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
