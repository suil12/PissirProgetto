namespace MobiShare.Core.Interfaces
{
    public interface IDeviceMappingService
    {
        Task<DeviceMapping?> GetDeviceByIdAsync(string deviceId);
        Task<List<DeviceMapping>> GetDevicesByTipoAsync(TipoDispositivo tipo);
        Task<List<DeviceMapping>> GetDevicesByMezzoAsync(string mezzoId);
        Task<List<DeviceMapping>> GetDevicesByParcheggioAsync(string parcheggioId);
        Task<bool> RegisterDeviceAsync(DeviceMapping device);
        Task<bool> UpdateDeviceStatusAsync(string deviceId, bool isAttivo);
        Task<bool> RemoveDeviceAsync(string deviceId);
        string GenerateTopicForDevice(string deviceId);
        string GenerateCommandTopicForDevice(string deviceId);
    }
}