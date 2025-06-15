namespace MobiShare.Core.Enums
{
    /// <summary>
    /// Enum per identificare i tipi di dispositivi IoT nel sistema
    /// </summary>
    public enum TipoDispositivo
    {
        // Dispositivi sui mezzi
        SensoreBatteriaMezzo = 1,
        AttuatoreBloccoMezzo = 2,
        SensoreGpsMezzo = 3,

        // Dispositivi nei parcheggi
        SensoreLuceSlot = 10,
        AttuatoreLuceSlot = 11,
        SensoreOccupazioneSlot = 12,

        // Dispositivi di sistema
        GatewayParcheggio = 20,
        SensoreMeteo = 21,
        CameraSicurezza = 22
    }
}