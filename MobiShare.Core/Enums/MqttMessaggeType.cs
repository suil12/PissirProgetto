namespace MobiShare.Core.Enums
{
    public enum MqttMessageType
    {
        VehicleStatusUpdate,
        VehicleBatteryLow,
        VehicleMaintenanceAlert,
        VehicleLockCommand,
        VehicleUnlockCommand,
        VehicleLocateRequest,
        SlotSensorUpdate,
        ParkingStatusUpdate,
        SystemNotification,
        EmergencyAlert
    }
}