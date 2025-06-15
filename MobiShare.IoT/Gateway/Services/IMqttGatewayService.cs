using MobiShare.Core.Models;

namespace MobiShare.IoT.Gateway.Services
{
    public interface IMqttGatewayService
    {
        Task<bool> StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
        Task<bool> PublishToDeviceAsync(string deviceId, object payload);
        Task<bool> SendCommandToDeviceAsync(string deviceId, string command, object payload);
        event EventHandler<DeviceDataReceivedEventArgs> DeviceDataReceived;
        event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;
    }

    public class DeviceDataReceivedEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}