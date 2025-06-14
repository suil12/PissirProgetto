using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobiShare.API.DTOs;
using MobiShare.Core.Interfaces;
using System.Security.Claims;

namespace MobiShare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UtentiController : ControllerBase
    {
        private readonly IUtenteService _utenteService;

        public UtentiController(IUtenteService utenteService)
        {
            _utenteService = utenteService;
        }

        [HttpGet("profilo")]
        public async Task<ActionResult<UtenteDto>> GetProfilo()
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

            var utente = await _utenteService.GetUtenteByIdAsync(utenteId);
            if (utente == null)
                return NotFound();

            return Ok(new UtenteDto
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Tipo = utente.Tipo,
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato,
                DataRegistrazione = utente.DataRegistrazione
            });
        }

        [HttpPost("credito")]
        public async Task<ActionResult> AggiornCredito(AggiornaCreditoDto dto)
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

            var success = await _utenteService.UpdateCreditoAsync(utenteId, dto.Importo);
            if (!success)
                return BadRequest("Impossibile aggiornare il credito");

            return Ok();
        }

        [HttpPost("converti-punti")]
        public async Task<ActionResult<object>> ConvertiPuntiEco(ConvertiPuntiDto dto)
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

            var buono = await _utenteService.ConvertiPuntiEcoAsync(utenteId, dto.PuntiDaConvertire);
            if (buono == null)
                return BadRequest("Punti insufficienti o non validi per la conversione");

            return Ok(new { BuonoId = buono.Id, Valore = buono.Valore });
        }

        [HttpGet("corse")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetCorseUtente()
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

            var corse = await _utenteService.GetCorseUtenteAsync(utenteId);
            var corseDtos = corse.Select(c => new CorsaDto
            {
                Id = c.Id,
                UtenteId = c.UtenteId,
                MezzoId = c.MezzoId,
                DataInizio = c.DataInizio,
                DataFine = c.DataFine,
                Durata = c.Durata,
                Costo = c.Costo,
                PuntiEcoAccumulati = c.PuntiEcoAccumulati,
                Stato = c.Stato
            });

            return Ok(corseDtos);
        }

        [HttpGet("buoni-sconto")]
        public async Task<ActionResult<IEnumerable<object>>> GetBuoniScontoUtente()
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

            var buoni = await _utenteService.GetBuoniScontoUtenteAsync(utenteId);
            var buoniDtos = buoni.Select(b => new
            {
                Id = b.Id,
                Valore = b.Valore,
                DataCreazione = b.DataCreazione,
                DataScadenza = b.DataScadenza,
                Stato = b.Stato
            });

            return Ok(buoniDtos);
        }
    }
}
