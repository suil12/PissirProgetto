using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MobiShare.Core.Interfaces;
using MobiShare.Core.Models;
using MobiShare.Core.Enums;

namespace MobiShare.IoT.Gateway.Services
{
    /// <summary>
    /// Servizio che simula i dispositivi IoT per testing
    /// </summary>
    public class DeviceSimulatorService : BackgroundService
    {
        private readonly IMqttGatewayService _gatewayService;
        private readonly IDeviceMappingService _deviceMapping;
        private readonly ILogger<DeviceSimulatorService> _logger;
        private readonly Random _random = new();

        public DeviceSimulatorService(
            IMqttGatewayService gatewayService,
            IDeviceMappingService deviceMapping,
            ILogger<DeviceSimulatorService> logger)
        {
            _gatewayService = gatewayService;
            _deviceMapping = deviceMapping;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Attendi che il gateway sia connesso
            while (!_gatewayService.IsConnected && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Avvio simulazione dispositivi IoT...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await SimulateBatteryLevel();
                    await SimulateGpsPosition();
                    await SimulateSlotStatus();

                    await Task.Delay(15000, stoppingToken); // Ogni 15 secondi
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Simulazione dispositivi arrestata");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella simulazione dispositivi");
            }
        }

        private async Task SimulateBatteryLevel()
        {
            try
            {
                var batteryDevices = await _deviceMapping.GetDevicesByTipoAsync(TipoDispositivo.SensoreBatteriaMezzo);

                foreach (var device in batteryDevices)
                {
                    var batteryLevel = _random.Next(15, 101); // 15-100%
                    var batteryData = new
                    {
                        DeviceId = device.DeviceId,
                        MezzoId = device.MezzoId,
                        BatteryLevel = batteryLevel,
                        Voltage = Math.Round(12.0 + (batteryLevel * 0.015), 2), // 12.0V - 13.5V
                        Temperature = _random.Next(-10, 45),
                        Timestamp = DateTime.UtcNow,
                        IsCharging = batteryLevel < 20 ? _random.NextDouble() > 0.7 : false
                    };

                    await _gatewayService.PublishToDeviceAsync(device.DeviceId, batteryData);

                    if (batteryLevel < 20)
                    {
                        _logger.LogWarning("Batteria scarica rilevata: Mezzo {MezzoId} - {BatteryLevel}%",
                            device.MezzoId, batteryLevel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella simulazione livello batteria");
            }
        }

        private async Task SimulateGpsPosition()
        {
            try
            {
                var gpsDevices = await _deviceMapping.GetDevicesByTipoAsync(TipoDispositivo.SensoreGpsMezzo);

                foreach (var device in gpsDevices)
                {
                    // Simula movimento GPS a Torino
                    var baseLatitude = 45.0703 + (_random.NextDouble() - 0.5) * 0.01; // ±0.01 gradi
                    var baseLongitude = 7.6869 + (_random.NextDouble() - 0.5) * 0.01;

                    var positionData = new
                    {
                        DeviceId = device.DeviceId,
                        MezzoId = device.MezzoId,
                        Latitude = Math.Round(baseLatitude, 6),
                        Longitude = Math.Round(baseLongitude, 6),
                        Altitude = _random.Next(200, 400),
                        Speed = _random.Next(0, 25), // 0-25 km/h
                        Heading = _random.Next(0, 360),
                        Accuracy = _random.Next(3, 15), // metri
                        Timestamp = DateTime.UtcNow,
                        SatelliteCount = _random.Next(6, 12)
                    };

                    await _gatewayService.PublishToDeviceAsync(device.DeviceId, positionData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella simulazione posizione GPS");
            }
        }

        private async Task SimulateSlotStatus()
        {
            try
            {
                var slotDevices = await _deviceMapping.GetDevicesByTipoAsync(TipoDispositivo.SensoreLuceSlot);

                foreach (var device in slotDevices)
                {
                    var isOccupied = _random.NextDouble() > 0.7; // 30% occupazione
                    var lightColor = isOccupied ? ColoreLuce.Rosso : ColoreLuce.Verde;

                    var slotData = new
                    {
                        DeviceId = device.DeviceId,
                        SlotId = device.SlotId,
                        ParcheggioId = device.ParcheggioId,
                        IsOccupied = isOccupied,
                        LightColor = lightColor.ToString(),
                        LightIntensity = _random.Next(80, 100),
                        SensorActive = true,
                        LastMovement = isOccupied ? DateTime.UtcNow.AddMinutes(-_random.Next(1, 30)) : (DateTime?)null,
                        Timestamp = DateTime.UtcNow
                    };

                    await _gatewayService.PublishToDeviceAsync(device.DeviceId, slotData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella simulazione stato slot");
            }
        }
    }
}
