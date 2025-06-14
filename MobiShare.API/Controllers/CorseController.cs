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
    public class CorseController : ControllerBase
    {
        private readonly ICorsaService _corsaService;

        public CorseController(ICorsaService corsaService)
        {
            _corsaService = corsaService;
        }

        [HttpPost("inizia")]
        public async Task<ActionResult<CorsaDto>> IniziaCorsa(IniziaCorsaDto dto)
        {
            var utenteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(utenteId))
                return Unauthorized();

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

        [HttpPut("{id}/termina")]
        public async Task<ActionResult<CorsaDto>> TerminaCorsa(string id, TerminaCorsaDto dto)
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

        [HttpGet("attiva")]
        public async Task<ActionResult<CorsaDto>> GetCorsaAttiva()
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

        [HttpGet("storico")]
        public async Task<ActionResult<IEnumerable<CorsaDto>>> GetStoricoRiode()
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
    }
}