namespace MobiShare.API.Services
{
    /// <summary>
    /// Servizio per inviare comandi al microservizio MQTT Gateway
    /// </summary>
    public interface IIoTCommandService
    {
        Task<bool> BlockVehicleAsync(string mezzoId);
        Task<bool> UnblockVehicleAsync(string mezzoId);
        Task<bool> ChangeSlotLightColorAsync(string slotId, ColoreLuce colore);
        Task<bool> RequestVehicleLocationAsync(string mezzoId);
        Task<bool> SendMaintenanceCommandAsync(string mezzoId, string command);
    }
}
