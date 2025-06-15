namespace MobiShare.IoT.Models
{
    public class MqttConfig
    {
        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string ClientId { get; set; } = "MobiShare-IoT";
        public int KeepAlive { get; set; } = 60;
        public bool CleanSession { get; set; } = true;
    }
}
