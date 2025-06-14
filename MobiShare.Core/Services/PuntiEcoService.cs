using MobiShare.Core.Entities;
using MobiShare.Core.Enums;
using MobiShare.Core.Interfaces;

namespace MobiShare.Core.Services
{
    public class PuntiEcoService : IPuntiEcoService
    {
        private const int PuntiPerMinutoMuscolare = 2;
        private const int PuntiPerConversioneSconto = 100;
        private const decimal ValoreScontoPer100Punti = 2.00m;

        private readonly IUtenteRepository _utenteRepository;

        public PuntiEcoService(IUtenteRepository utenteRepository)
        {
            _utenteRepository = utenteRepository;
        }

        public int CalcolaPuntiEco(Corsa corsa)
        {
            if (corsa.Mezzo?.Tipo == TipoMezzo.BiciMuscolare && corsa.DataFine.HasValue)
            {
                var durataMinuti = (int)(corsa.DataFine.Value - corsa.DataInizio).TotalMinutes;
                return durataMinuti * PuntiPerMinutoMuscolare;
            }
            return 0;
        }

        public async Task<BuonoSconto?> ConvertiPuntiInScontoAsync(string utenteId, int puntiDaConvertire)
        {
            if (!PuoConvertirePunti(puntiDaConvertire))
                return null;

            var utente = await _utenteRepository.GetByIdAsync(utenteId);
            if (utente == null || utente.PuntiEco < puntiDaConvertire)
                return null;

            var valoreSconto = CalcolaValoreSconto(puntiDaConvertire);

            var buono = new BuonoSconto
            {
                Valore = valoreSconto,
                UtenteId = utenteId,
                DataScadenza = DateTime.UtcNow.AddMonths(6),
                Stato = StatoBuono.Valido
            };

            // Sottrai punti dall'utente
            await _utenteRepository.UpdatePuntiEcoAsync(utenteId, utente.PuntiEco - puntiDaConvertire);

            utente.BuoniSconto.Add(buono);
            await _utenteRepository.UpdateAsync(utente);

            return buono;
        }

        public decimal CalcolaValoreSconto(int punti)
        {
            var moltiplicatore = punti / PuntiPerConversioneSconto;
            return ValoreScontoPer100Punti * moltiplicatore;
        }

        public bool PuoConvertirePunti(int punti)
        {
            return punti >= PuntiPerConversioneSconto && punti % PuntiPerConversioneSconto == 0;
        }
    }
}