using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobiShare.API.DTOs;
using MobiShare.Core.Entities;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Enums;

namespace MobiShare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MezziController : ControllerBase
    {
        private readonly IMezzoService _mezzoService;

        public MezziController(IMezzoService mezzoService)
        {
            _mezzoService = mezzoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MezzoDto>>> GetMezzi([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double raggio = 1.0)
        {
            IEnumerable<Mezzo> mezzi;

            if (lat.HasValue && lng.HasValue)
            {
                mezzi = await _mezzoService.GetMezziDisponibiliVicinoAsync(lat.Value, lng.Value, raggio);
            }
            else
            {
                mezzi = await _mezzoService.GetTuttiMezziAsync();
            }

            var mezziDtos = mezzi.Select(m => new MezzoDto
            {
                Id = m.Id,
                Tipo = m.Tipo,
                Modello = m.Modello,
                Stato = m.Stato,
                PercentualeBatteria = m.PercentualeBatteria,
                TariffaPerMinuto = m.TariffaPerMinuto,
                Latitudine = m.Latitudine,
                Longitudine = m.Longitudine,
                ParcheggioDiPartenzaId = m.ParcheggioDiPartenzaId,
                SlotId = m.SlotId,
                UltimaManutenzione = m.UltimaManutenzione
            });

            return Ok(mezziDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MezzoDto>> GetMezzo(string id)
        {
            var mezzo = await _mezzoService.GetMezzoByIdAsync(id);
            if (mezzo == null)
                return NotFound();

            var mezzoDto = new MezzoDto
            {
                Id = mezzo.Id,
                Tipo = mezzo.Tipo,
                Modello = mezzo.Modello,
                Stato = mezzo.Stato,
                PercentualeBatteria = mezzo.PercentualeBatteria,
                TariffaPerMinuto = mezzo.TariffaPerMinuto,
                Latitudine = mezzo.Latitudine,
                Longitudine = mezzo.Longitudine,
                ParcheggioDiPartenzaId = mezzo.ParcheggioDiPartenzaId,
                SlotId = mezzo.SlotId,
                UltimaManutenzione = mezzo.UltimaManutenzione
            };

            return Ok(mezzoDto);
        }

        [HttpPost]
        [Authorize(Roles = "Gestore")]
        public async Task<ActionResult<MezzoDto>> CreaMezzo(CreaMezzoDto dto)
        {
            var mezzo = await _mezzoService.CreaMezzoAsync(dto.Tipo, dto.Modello, dto.TariffaPerMinuto, dto.ParcheggioId);

            var mezzoDto = new MezzoDto
            {
                Id = mezzo.Id,
                Tipo = mezzo.Tipo,
                Modello = mezzo.Modello,
                Stato = mezzo.Stato,
                PercentualeBatteria = mezzo.PercentualeBatteria,
                TariffaPerMinuto = mezzo.TariffaPerMinuto,
                Latitudine = mezzo.Latitudine,
                Longitudine = mezzo.Longitudine,
                ParcheggioDiPartenzaId = mezzo.ParcheggioDiPartenzaId
            };

            return CreatedAtAction(nameof(GetMezzo), new { id = mezzo.Id }, mezzoDto);
        }

        [HttpPut("{id}/stato")]
        [Authorize(Roles = "Gestore")]
        public async Task<ActionResult> AggiornaStatoMezzo(string id, AggiornaStatoMezzoDto dto)
        {
            var success = await _mezzoService.UpdateStatoMezzoAsync(id, dto.Stato);
            if (!success)
                return NotFound();

            return Ok();
        }

        [HttpPut("{id}/batteria")]
        public async Task<ActionResult> AggiornaBatteriaMezzo(string id, AggiornaBatteriaMezzoDto dto)
        {
            var success = await _mezzoService.UpdateBatteriaMezzoAsync(id, dto.PercentualeBatteria);
            if (!success)
                return NotFound();

            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Gestore")]
        public async Task<ActionResult> EliminaMezzo(string id)
        {
            var success = await _mezzoService.EliminaMezzoAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
