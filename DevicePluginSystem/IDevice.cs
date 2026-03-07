namespace DevicePluginSystem
{
    // Базовый класс для данных
    public abstract class DeviceData
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Пример конкретных данных
    public class TemperatureData : DeviceData
    {
        public double Value { get; set; }
    }

    // Интерфейс событий (Шина данных)
    public interface IDeviceEvents
    {
        void Publish<T>(string deviceName, T data) where T : DeviceData;
    }

    // Общий логгер
    public interface ILogger { void Log(string msg); }

    // Главный интерфейс устройства
    public interface IDevice
    {
        string Name { get; }
        void Connect();
    }
}
