namespace MobiShare.IoT.Gateway.Services
{
    /// <summary>
    /// Servizio per comunicare con il backend via API REST
    /// </summary>
    public interface IBackendApiService
    {
        Task<bool> UpdateBatteryLevelAsync(string mezzoId, object? batteryData);
        Task<bool> UpdateVehiclePositionAsync(string mezzoId, object? positionData);
        Task<bool> UpdateSlotStatusAsync(string slotId, object? statusData);
        Task<bool> UpdateParkingGatewayStatusAsync(string parcheggioId, object? gatewayData);
        Task<bool> SendMaintenanceAlertAsync(string mezzoId, string alertMessage);
        Task<bool> SendSystemNotificationAsync(string message, object? metadata);
    }
}

