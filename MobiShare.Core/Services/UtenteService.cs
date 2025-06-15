using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class UtenteService : IUtenteService
    {
        private readonly IUtenteRepository _utenteRepository;
        private readonly ICorsaRepository _corsaRepository;
        private readonly IPuntiEcoService _puntiEcoService;

        public UtenteService(IUtenteRepository utenteRepository, ICorsaRepository corsaRepository, IPuntiEcoService puntiEcoService)
        {
            _utenteRepository = utenteRepository;
            _corsaRepository = corsaRepository;
            _puntiEcoService = puntiEcoService;
        }

        public async Task<Utente?> RegistraAsync(string username, string email, string password, TipoUtente tipo)
        {
            // Verifica se username o email già esistono
            if (await _utenteRepository.GetByUsernameAsync(username) != null)
                return null;

            if (await _utenteRepository.GetByEmailAsync(email) != null)
                return null;

            var utente = new Utente
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Tipo = tipo,
                Credito = tipo == TipoUtente.Cliente ? 5.00m : 0m, // Bonus registrazione
                Stato = StatoUtente.Attivo
            };

            return await _utenteRepository.AddAsync(utente);
        }

        public async Task<Utente?> LoginAsync(string username, string password)
        {
            var utente = await _utenteRepository.GetByUsernameAsync(username);
            if (utente == null || !BCrypt.Net.BCrypt.Verify(password, utente.PasswordHash))
                return null;

            if (utente.Stato != StatoUtente.Attivo)
                return null;

            return utente;
        }

        public async Task<Utente?> GetUtenteByIdAsync(string utenteId)
        {
            return await _utenteRepository.GetByIdAsync(utenteId);
        }

        public async Task<bool> UpdateCreditoAsync(string utenteId, decimal importo)
        {
            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            if (utente == null || utente.Credito + importo < 0)
                return false;

            return await _utenteRepository.UpdateCreditoAsync(utenteId, utente.Credito + importo);
        }

        public async Task<BuonoSconto?> ConvertiPuntiEcoAsync(string utenteId, int puntiDaConvertire)
        {
            return await _puntiEcoService.ConvertiPuntiInScontoAsync(utenteId, puntiDaConvertire);
        }

        public async Task<IEnumerable<Corsa>> GetCorseUtenteAsync(string utenteId)
        {
            return await _corsaRepository.GetByUtenteAsync(utenteId);
        }

        public async Task<IEnumerable<BuonoSconto>> GetBuoniScontoUtenteAsync(string utenteId)
        {
            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            return utente?.BuoniSconto.Where(b => b.Stato == StatoBuono.Valido && b.DataScadenza > DateTime.UtcNow) ?? new List<BuonoSconto>();
        }

        public async Task<bool> UsaBuonoScontoAsync(string utenteId, string buonoScontoId)
        {
            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            var buono = utente?.BuoniSconto.FirstOrDefault(b => b.Id == buonoScontoId);

            if (buono == null || buono.Stato != StatoBuono.Valido || buono.DataScadenza <= DateTime.UtcNow)
                return false;

            // Applica sconto al credito
            await UpdateCreditoAsync(utenteId, buono.Valore);
            buono.Stato = StatoBuono.Utilizzato;

            if (utente != null)
                await _utenteRepository.UpdateAsync(utente);
            return true;
        }

        public async Task<bool> VerificaCreditoSufficienteAsync(string utenteId, decimal importoRichiesto)
        {
            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            return utente != null && utente.Credito >= importoRichiesto;
        }
    }
}