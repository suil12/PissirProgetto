using MobiShare.Core.Entities;

namespace MobiShare.Core.Interfaces
{
    public interface IPuntiEcoService
    {
        int CalcolaPuntiEco(Corsa corsa);
        Task<BuonoSconto?> ConvertiPuntiInScontoAsync(string utenteId, int puntiDaConvertire);
        decimal CalcolaValoreSconto(int punti);
        bool PuoConvertirePunti(int punti);

        // Metodi per compatibilità - corretti con async
        decimal CalcolaPuntiEco(TimeSpan durata, bool isBiciMuscolare);
        Task<BuonoSconto?> ConvertiPuntiInBuono(string utenteId, int punti);
        decimal GetValoreBuonoByPunti(int punti);
        Task<bool> VerificaPuntiSufficienti(string utenteId, int puntiRichiesti);
        Task<bool> AggiornaPuntiUtenteAsync(string utenteId, int nuoviPunti);
    }
}