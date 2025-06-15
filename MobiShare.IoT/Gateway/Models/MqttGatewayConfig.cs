namespace MobiShare.IoT.Gateway.Models
{
    public class MqttGatewayConfig
    {
        public string BrokerHost { get; set; } = "localhost";
        public int BrokerPort { get; set; } = 1883;
        public string ClientId { get; set; } = "MobiShare-IoT-Gateway";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool CleanSession { get; set; } = true;
        public int KeepAlivePeriod { get; set; } = 60;
        public string BackendApiUrl { get; set; } = "http://localhost:5000";
        public string BackendApiKey { get; set; } = string.Empty;
    }
}
