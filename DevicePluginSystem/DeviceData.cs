namespace DevicePluginSystem
{
    // Базовый класс для данных
    public abstract class DeviceData
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
