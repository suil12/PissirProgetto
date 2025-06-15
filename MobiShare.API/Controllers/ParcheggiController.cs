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
    public class ParcheggiController : ControllerBase
    {
        private readonly IParcheggioService _parcheggioService;

        public ParcheggiController(IParcheggioService parcheggioService)
        {
            _parcheggioService = parcheggioService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParcheggioDto>>> GetParcheggi([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double raggio = 2.0)
        {
            IEnumerable<Parcheggio> parcheggi;

            if (lat.HasValue && lng.HasValue)
            {
                parcheggi = await _parcheggioService.GetParcheggiViciniAsync(lat.Value, lng.Value, raggio);
            }
            else
            {
                parcheggi = await _parcheggioService.GetTuttiParcheggiAsync();
            }

            var parcheggiDtos = parcheggi.Select(p => new ParcheggioDto
            {
                Id = p.Id,
                Nome = p.Nome,
                Latitudine = p.Latitudine,
                Longitudine = p.Longitudine,
                Capacita = p.Capacita,
                SlotsDisponibili = p.Slots?.Count(s => s.Stato == StatoSlot.Libero) ?? 0
            });

            return Ok(parcheggiDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ParcheggioDto>> GetParcheggioConDettagli(string id)
        {
            var parcheggio = await _parcheggioService.GetParcheggioConDettagliAsync(id);
            if (parcheggio == null)
                return NotFound();

            var parcheggioDto = new ParcheggioDto
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Latitudine = parcheggio.Latitudine,
                Longitudine = parcheggio.Longitudine,
                Capacita = parcheggio.Capacita,
                SlotsDisponibili = parcheggio.Slots?.Count(s => s.Stato == StatoSlot.Libero) ?? 0,
                Slots = parcheggio.Slots?.Select(s => new SlotDto
                {
                    Id = s.Id,
                    Numero = s.Numero,
                    Stato = s.Stato,
                    ColoreLED = s.SensoreLuce?.Colore ?? ColoreLuce.Verde,
                    MezzoId = s.MezzoId,
                    UltimoAggiornamento = s.UltimoAggiornamento
                }).ToList() ?? new List<SlotDto>(),
                MezziPresenti = parcheggio.MezziPresenti?.Select(m => new MezzoDto
                {
                    Id = m.Id,
                    Tipo = m.Tipo,
                    Modello = m.Modello,
                    Stato = m.Stato,
                    PercentualeBatteria = m.PercentualeBatteria,
                    TariffaPerMinuto = m.TariffaPerMinuto
                }).ToList() ?? new List<MezzoDto>()
            };

            return Ok(parcheggioDto);
        }

        [HttpPost]
        [Authorize(Roles = "Gestore")]
        public async Task<ActionResult<ParcheggioDto>> CreaParcheggio(CreaParcheggioDto dto)
        {
            var parcheggio = await _parcheggioService.CreaParcheggioAsync(dto.Nome, dto.Indirizzo, dto.Latitudine, dto.Longitudine, dto.Capacita);

            var parcheggioDto = new ParcheggioDto
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Latitudine = parcheggio.Latitudine,
                Longitudine = parcheggio.Longitudine,
                Capacita = parcheggio.Capacita,
                SlotsDisponibili = parcheggio.Capacita
            };

            return CreatedAtAction(nameof(GetParcheggioConDettagli), new { id = parcheggio.Id }, parcheggioDto);
        }
    }
}