using Microsoft.AspNetCore.Mvc;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MobiShare.API.DTOs;

namespace MobiShare.API.Controllers
{
    [ApiController]
    [Route("api/corse")]
    [Authorize]
    public class CorsaController : ControllerBase
    {
        private readonly ILogger<CorsaController> _logger;
        private readonly ICorsaService _corsaService;
        private readonly IMezzoService _mezzoService;
        private readonly IUtenteService _utenteService;
        private readonly IIoTCommandService _iotCommandService;
        private readonly IMqttService _mqttService;

        public CorsaController(
            ILogger<CorsaController> logger,
            ICorsaService corsaService,
            IMezzoService mezzoService,
            IUtenteService utenteService,
            IIoTCommandService iotCommandService,
            IMqttService mqttService)
        {
            _logger = logger;
            _corsaService = corsaService;
            _mezzoService = mezzoService;
            _utenteService = utenteService;
            _iotCommandService = iotCommandService;
            _mqttService = mqttService;
        }

        /// <summary>
        /// Ottiene tutte le corse con filtri opzionali
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Corsa>>> GetCorse(
            [FromQuery] string? utenteId = null,
            [FromQuery] string? mezzoId = null,
            [FromQuery] StatoCorsa? stato = null,
            [FromQuery] DateTime? dataInizio = null,
            [FromQuery] DateTime? dataFine = null)
        {
            try
            {
                var corse = await _corsaService.GetCorseAsync(utenteId, mezzoId, stato, dataInizio, dataFine);
                return Ok(corse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero delle corse");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottiene una corsa specifica per ID
        /// </summary>
        [HttpGet("{corsaId}")]
        public async Task<ActionResult<Corsa>> GetCorsa(string corsaId)
        {
            try
            {
                var corsa = await _corsaService.GetByIdAsync(corsaId);
                if (corsa == null)
                {
                    return NotFound($"Corsa {corsaId} non trovata");
                }

                return Ok(corsa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero corsa {CorsaId}", corsaId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Inizia una nuova corsa con integrazione MQTT
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Corsa>> IniziaCorsa([FromBody] InizioCorsa request)
        {
            try
            {
                _logger.LogInformation("Richiesta inizio corsa: Utente {UtenteId}, Mezzo {MezzoId}",
                    request.UtenteId, request.MezzoId);

                // 1. Validazioni preliminari
                var utente = await _utenteService.GetByIdAsync(request.UtenteId);
                if (utente == null)
                {
                    return BadRequest("Utente non trovato");
                }

                var mezzo = await _mezzoService.GetByIdAsync(request.MezzoId);
                if (mezzo == null)
                {
                    return BadRequest("Mezzo non trovato");
                }

                if (mezzo.Stato != StatoMezzo.Disponibile)
                {
                    return BadRequest($"Mezzo non disponibile. Stato attuale: {mezzo.Stato}");
                }

                if (utente.Credito < 2.00m)
                {
                    return BadRequest("Credito insufficiente per iniziare la corsa (minimo €2.00)");
                }

                // 2. Controlla se l'utente ha già una corsa in corso
                var corsaInCorso = await _corsaService.GetCorsaInCorsoByUtenteAsync(request.UtenteId);
                if (corsaInCorso != null)
                {
                    return BadRequest($"Utente ha già una corsa in corso: {corsaInCorso.Id}");
                }

                // 3. COMANDO FISICO: Sblocca il mezzo tramite MQTT
                var sbloccaResult = await _iotCommandService.UnblockVehicleAsync(
                    request.MezzoId,
                    string.Empty, // CorsaId sarà disponibile dopo la creazione
                    request.UtenteId
                );

                if (!sbloccaResult)
                {
                    _logger.LogError("Impossibile sbloccare il mezzo {MezzoId} tramite MQTT", request.MezzoId);
                    return StatusCode(500, "Errore nello sblocco del mezzo. Riprovare.");
                }

                _logger.LogInformation("Mezzo {MezzoId} sbloccato fisicamente tramite MQTT", request.MezzoId);

                // 4. Crea la corsa nel database
                var corsa = await _corsaService.IniziaCorsa(request.UtenteId, request.MezzoId);
                if (corsa == null)
                {
                    // Se la creazione della corsa fallisce, prova a ribloccare il mezzo
                    await _iotCommandService.BlockVehicleAsync(request.MezzoId);
                    return StatusCode(500, "Errore nella creazione della corsa");
                }

                // 5. Pubblica evento inizio corsa su MQTT
                await _mqttService.PubblicaEventoInizioCorsaAsync(
                    corsa.Id,
                    request.MezzoId,
                    request.UtenteId
                );

                // 6. Aggiorna comando sblocco con CorsaId ora disponibile
                await _iotCommandService.UnblockVehicleAsync(request.MezzoId, corsa.Id, request.UtenteId);

                _logger.LogInformation("Corsa {CorsaId} iniziata con successo per utente {UtenteId} con mezzo {MezzoId}",
                    corsa.Id, request.UtenteId, request.MezzoId);

                return Ok(corsa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'inizio corsa per utente {UtenteId}", request.UtenteId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Termina una corsa con integrazione MQTT
        /// </summary>
        [HttpPut("{corsaId}/termina")]
        public async Task<ActionResult<Corsa>> TerminaCorsa(string corsaId, [FromBody] FineCorsa request)
        {
            try
            {
                _logger.LogInformation("Richiesta fine corsa {CorsaId} al parcheggio {ParcheggioId}",
                    corsaId, request.ParcheggioDestinazioneId);

                // 1. Recupera corsa esistente
                var corsa = await _corsaService.GetByIdAsync(corsaId);
                if (corsa == null)
                {
                    return NotFound($"Corsa {corsaId} non trovata");
                }

                if (corsa.Stato != StatoCorsa.InCorso)
                {
                    return BadRequest($"La corsa non è in corso. Stato attuale: {corsa.Stato}");
                }

                // 2. Recupera mezzo
                var mezzo = await _mezzoService.GetByIdAsync(corsa.MezzoId);
                if (mezzo == null)
                {
                    return BadRequest("Mezzo della corsa non trovato");
                }

                // 3. COMANDO FISICO: Blocca il mezzo tramite MQTT
                var bloccaResult = await _iotCommandService.BlockVehicleAsync(corsa.MezzoId, corsaId);
                if (!bloccaResult)
                {
                    _logger.LogError("Impossibile bloccare il mezzo {MezzoId} tramite MQTT", corsa.MezzoId);
                    return StatusCode(500, "Errore nel blocco del mezzo. Contattare l'assistenza.");
                }

                _logger.LogInformation("Mezzo {MezzoId} bloccato fisicamente tramite MQTT", corsa.MezzoId);

                // 4. Termina corsa nel database (include gestione slot e LED)
                var corsaTerminata = await _corsaService.TerminaCorsa(corsaId, request.ParcheggioDestinazioneId);
                if (corsaTerminata == null)
                {
                    return StatusCode(500, "Errore nella terminazione della corsa");
                }

                // 5. Pubblica evento fine corsa su MQTT
                await _mqttService.PubblicaEventoFineCorsaAsync(
                    corsaId,
                    corsa.MezzoId,
                    TimeSpan.FromMinutes(corsaTerminata.Durata),
                    corsaTerminata.Costo
                );

                _logger.LogInformation("Corsa {CorsaId} terminata con successo. Costo: €{Costo}",
                    corsaId, corsaTerminata.Costo);

                return Ok(corsaTerminata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella terminazione corsa {CorsaId}", corsaId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Annulla una corsa in corso
        /// </summary>
        [HttpPut("{corsaId}/annulla")]
        public async Task<ActionResult<Corsa>> AnnullaCorsa(string corsaId, [FromBody] AnnullamentoCorsa request)
        {
            try
            {
                _logger.LogInformation("Richiesta annullamento corsa {CorsaId}. Motivo: {Motivo}",
                    corsaId, request.Motivo);

                var corsa = await _corsaService.GetByIdAsync(corsaId);
                if (corsa == null)
                {
                    return NotFound($"Corsa {corsaId} non trovata");
                }

                if (corsa.Stato != StatoCorsa.InCorso)
                {
                    return BadRequest($"La corsa non può essere annullata. Stato attuale: {corsa.Stato}");
                }

                // 1. COMANDO FISICO: Blocca il mezzo
                await _iotCommandService.BlockVehicleAsync(corsa.MezzoId, corsaId);

                // 2. Annulla corsa nel database
                var corsaAnnullata = await _corsaService.AnnullaCorsa(corsaId, request.Motivo);
                if (corsaAnnullata == null)
                {
                    return StatusCode(500, "Errore nell'annullamento della corsa");
                }

                // 3. Pubblica notifica annullamento
                await _mqttService.PubblicaNotificaSistemaAsync(
                    $"Corsa {corsaId} annullata: {request.Motivo}",
                    new { corsaId, motivo = request.Motivo, timestamp = DateTime.UtcNow }
                );

                return Ok(corsaAnnullata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'annullamento corsa {CorsaId}", corsaId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottiene le corse attive per monitoraggio
        /// </summary>
        [HttpGet("attive")]
        public async Task<ActionResult<IEnumerable<object>>> GetCorseAttive()
        {
            try
            {
                var corseAttive = await _corsaService.GetCorseAtttiveAsync();

                var result = corseAttive.Select(corsa => new
                {
                    corsa.Id,
                    corsa.UtenteId,
                    corsa.MezzoId,
                    corsa.ParcheggioDiPartenzaId,
                    corsa.DataInizio,
                    DurataMinuti = (DateTime.UtcNow - corsa.DataInizio).TotalMinutes,
                    corsa.Stato
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero corse attive");
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottiene statistiche delle corse per un utente
        /// </summary>
        [HttpGet("utente/{utenteId}/statistiche")]
        public async Task<ActionResult<object>> GetStatisticheCorse(string utenteId)
        {
            try
            {
                var statistiche = await _corsaService.GetStatisticheUtenteAsync(utenteId);
                return Ok(statistiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero statistiche per utente {UtenteId}", utenteId);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Inizia una nuova corsa (endpoint compatibile con frontend)
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<CorsaDto>> StartRide(IniziaCorsaDto dto)
        {
            try
            {
                var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(utenteId))
                    return Unauthorized();

                _logger.LogInformation("Richiesta inizio corsa: Utente {UtenteId}, Mezzo {MezzoId}",
                    utenteId, dto.MezzoId);

                var corsa = await _corsaService.IniziaCorsaAsync(utenteId, dto.MezzoId);
                if (corsa == null)
                    return BadRequest("Impossibile iniziare la corsa");

                var corsaDto = new CorsaDto
                {
                    Id = corsa.Id,
                    UtenteId = corsa.UtenteId,
                    MezzoId = corsa.MezzoId,
                    ParcheggioDiPartenzaId = corsa.ParcheggioDiPartenzaId,
                    DataInizio = corsa.DataInizio,
                    Stato = corsa.Stato
                };

                return Ok(corsaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'avvio corsa per utente {UtenteId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottiene la corsa attiva dell'utente corrente
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<CorsaDto>> GetActiveRide()
        {
            try
            {
                var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(utenteId))
                    return Unauthorized();

                var corsa = await _corsaService.GetCorsaAttivaAsync(utenteId);
                if (corsa == null)
                    return NotFound();

                var corsaDto = new CorsaDto
                {
                    Id = corsa.Id,
                    UtenteId = corsa.UtenteId,
                    MezzoId = corsa.MezzoId,
                    ParcheggioDiPartenzaId = corsa.ParcheggioDiPartenzaId,
                    DataInizio = corsa.DataInizio,
                    Stato = corsa.Stato
                };

                return Ok(corsaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero corsa attiva per utente {UtenteId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Termina una corsa specifica
        /// </summary>
        [HttpPut("{id}/end")]
        public async Task<ActionResult<CorsaDto>> EndRide(string id, TerminaCorsaDto dto)
        {
            try
            {
                var corsa = await _corsaService.TerminaCorsaAsync(id, dto.ParcheggioDestinazioneId);
                if (corsa == null)
                    return BadRequest("Impossibile terminare la corsa");

                var corsaDto = new CorsaDto
                {
                    Id = corsa.Id,
                    UtenteId = corsa.UtenteId,
                    MezzoId = corsa.MezzoId,
                    ParcheggioDiPartenzaId = corsa.ParcheggioDiPartenzaId,
                    ParcheggioDestinazioneId = corsa.ParcheggioDestinazioneId,
                    DataInizio = corsa.DataInizio,
                    DataFine = corsa.DataFine,
                    Durata = corsa.Durata,
                    Costo = corsa.Costo,
                    PuntiEcoAccumulati = corsa.PuntiEcoAccumulati,
                    Stato = corsa.Stato
                };

                return Ok(corsaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella terminazione corsa {CorsaId}", id);
                return StatusCode(500, "Errore interno del server");
            }
        }

        /// <summary>
        /// Ottiene lo storico delle corse dell'utente corrente
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetRideHistory()
        {
            try
            {
                var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(utenteId))
                    return Unauthorized();

                var corse = await _corsaService.GetStoricoCorseAsync(utenteId);
                var corseDtos = corse.Select(c => new CorsaDto
                {
                    Id = c.Id,
                    UtenteId = c.UtenteId,
                    MezzoId = c.MezzoId,
                    ParcheggioDiPartenzaId = c.ParcheggioDiPartenzaId,
                    ParcheggioDestinazioneId = c.ParcheggioDestinazioneId,
                    DataInizio = c.DataInizio,
                    DataFine = c.DataFine,
                    Durata = c.Durata,
                    Costo = c.Costo,
                    PuntiEcoAccumulati = c.PuntiEcoAccumulati,
                    Stato = c.Stato
                });

                return Ok(corseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero storico corse per utente {UtenteId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "Errore interno del server");
            }
        }

        // ===== REQUEST/RESPONSE MODELS =====

        public class InizioCorsa
        {
            public string UtenteId { get; set; } = string.Empty;
            public string MezzoId { get; set; } = string.Empty;
        }

        public class FineCorsa
        {
            public string ParcheggioDestinazioneId { get; set; } = string.Empty;
        }

        public class AnnullamentoCorsa
        {
            public string Motivo { get; set; } = string.Empty;
        }
    }
}