using Microsoft.AspNetCore.Mvc;
using MobiShare.API.DTOs;
using MobiShare.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MobiShare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUtenteService _utenteService;
        private readonly IConfiguration _configuration;

        public AuthController(IUtenteService utenteService, IConfiguration configuration)
        {
            _utenteService = utenteService;
            _configuration = configuration;
        }

        [HttpPost("registra")]
        public async Task<ActionResult<UtenteDto>> Registra(RegistraUtenteDto dto)
        {
            var utente = await _utenteService.RegistraAsync(dto.Username, dto.Email, dto.Password, dto.Tipo);
            if (utente == null)
                return BadRequest("Username o email già esistenti");

            var utenteDto = new UtenteDto
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Tipo = utente.Tipo,
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato,
                DataRegistrazione = utente.DataRegistrazione
            };

            return Ok(utenteDto);
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(LoginDto dto)
        {
            var utente = await _utenteService.LoginAsync(dto.Username, dto.Password);
            if (utente == null)
                return Unauthorized("Credenziali non valide");

            var token = GeneraTokenJwt(utente.Id, utente.Username, utente.Tipo.ToString());

            return Ok(new
            {
                Token = token,
                Utente = new UtenteDto
                {
                    Id = utente.Id,
                    Username = utente.Username,
                    Email = utente.Email,
                    Tipo = utente.Tipo,
                    Credito = utente.Credito,
                    PuntiEco = utente.PuntiEco,
                    Stato = utente.Stato,
                    DataRegistrazione = utente.DataRegistrazione
                }
            });
        }

        private string GeneraTokenJwt(string utenteId, string username, string tipoUtente)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-here-should-be-longer");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, utenteId),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, tipoUtente)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}