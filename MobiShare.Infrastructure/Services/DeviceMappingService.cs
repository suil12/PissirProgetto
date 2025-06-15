using MobiShare.Core.Interfaces;
using MobiShare.Core.Models;
using MobiShare.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MobiShare.Infrastructure.Services
{
    public class DeviceMappingService : IDeviceMappingService
    {
        private readonly ILogger<DeviceMappingService> _logger;
        private readonly Dictionary<string, DeviceMapping> _devices;
        private readonly object _lockObject = new object();

        public DeviceMappingService(ILogger<DeviceMappingService> logger)
        {
            _logger = logger;
            _devices = new Dictionary<string, DeviceMapping>();
            InitializeDefaultDevices();
        }

        public Task<DeviceMapping?> GetDeviceByIdAsync(string deviceId)
        {
            lock (_lockObject)
            {
                _devices.TryGetValue(deviceId, out var device);
                return Task.FromResult(device);
            }
        }

        public Task<List<DeviceMapping>> GetDevicesByTipoAsync(TipoDispositivo tipo)
        {
            lock (_lockObject)
            {
                var devices = _devices.Values
                    .Where(d => d.TipoDispositivo == tipo && d.IsAttivo)
                    .ToList();
                return Task.FromResult(devices);
            }
        }

        public Task<List<DeviceMapping>> GetDevicesByMezzoAsync(string mezzoId)
        {
            lock (_lockObject)
            {
                var devices = _devices.Values
                    .Where(d => d.MezzoId == mezzoId && d.IsAttivo)
                    .ToList();
                return Task.FromResult(devices);
            }
        }

        public Task<List<DeviceMapping>> GetDevicesByParcheggioAsync(string parcheggioId)
        {
            lock (_lockObject)
            {
                var devices = _devices.Values
                    .Where(d => d.ParcheggioId == parcheggioId && d.IsAttivo)
                    .ToList();
                return Task.FromResult(devices);
            }
        }

        public Task<bool> RegisterDeviceAsync(DeviceMapping device)
        {
            try
            {
                lock (_lockObject)
                {
                    _devices[device.DeviceId] = device;
                }

                _logger.LogInformation("Dispositivo registrato: {DeviceId} - {Tipo}",
                    device.DeviceId, device.TipoDispositivo);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella registrazione del dispositivo {DeviceId}", device.DeviceId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateDeviceStatusAsync(string deviceId, bool isAttivo)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_devices.TryGetValue(deviceId, out var device))
                    {
                        device.IsAttivo = isAttivo;
                        device.UltimaAttivita = DateTime.UtcNow;
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento dello stato del dispositivo {DeviceId}", deviceId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveDeviceAsync(string deviceId)
        {
            try
            {
                lock (_lockObject)
                {
                    var removed = _devices.Remove(deviceId);
                    if (removed)
                    {
                        _logger.LogInformation("Dispositivo rimosso: {DeviceId}", deviceId);
                    }
                    return Task.FromResult(removed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella rimozione del dispositivo {DeviceId}", deviceId);
                return Task.FromResult(false);
            }
        }

        public string GenerateTopicForDevice(string deviceId)
        {
            var device = _devices.GetValueOrDefault(deviceId);
            if (device == null) return $"mobishare/devices/{deviceId}/data";

            return device.TipoDispositivo switch
            {
                TipoDispositivo.SensoreBatteriaMezzo => $"mobishare/mezzi/{device.MezzoId}/batteria",
                TipoDispositivo.SensoreGpsMezzo => $"mobishare/mezzi/{device.MezzoId}/posizione",
                TipoDispositivo.AttuatoreBloccoMezzo => $"mobishare/mezzi/{device.MezzoId}/blocco",
                TipoDispositivo.SensoreLuceSlot => $"mobishare/parcheggi/{device.ParcheggioId}/slots/{device.SlotId}/stato",
                TipoDispositivo.AttuatoreLuceSlot => $"mobishare/parcheggi/{device.ParcheggioId}/slots/{device.SlotId}/led",
                TipoDispositivo.GatewayParcheggio => $"mobishare/parcheggi/{device.ParcheggioId}/gateway",
                _ => $"mobishare/devices/{deviceId}/data"
            };
        }

        public string GenerateCommandTopicForDevice(string deviceId)
        {
            var dataTopic = GenerateTopicForDevice(deviceId);
            return dataTopic.Replace("/data", "/commands").Replace("/stato", "/commands").Replace("/batteria", "/commands");
        }

        private void InitializeDefaultDevices()
        {
            // Esempi di dispositivi predefiniti per test
            var defaultDevices = new List<DeviceMapping>
            {
                new DeviceMapping
                {
                    DeviceId = "BAT_MEZZO001_1234",
                    TipoDispositivo = TipoDispositivo.SensoreBatteriaMezzo,
                    Nome = "Sensore Batteria Bici 001",
                    MezzoId = "MEZZO001"
                },
                new DeviceMapping
                {
                    DeviceId = "LOCK_MEZZO001_1235",
                    TipoDispositivo = TipoDispositivo.AttuatoreBloccoMezzo,
                    Nome = "Blocco Bici 001",
                    MezzoId = "MEZZO001"
                },
                new DeviceMapping
                {
                    DeviceId = "LED_PARK001_SLOT01_1236",
                    TipoDispositivo = TipoDispositivo.AttuatoreLuceSlot,
                    Nome = "LED Slot 01 Parcheggio 001",
                    ParcheggioId = "PARK001",
                    SlotId = "SLOT01"
                }
            };

            foreach (var device in defaultDevices)
            {
                _devices[device.DeviceId] = device;
            }

            _logger.LogInformation("Inizializzati {Count} dispositivi predefiniti", defaultDevices.Count);
        }
    }
}