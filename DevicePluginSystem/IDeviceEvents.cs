namespace DevicePluginSystem
{
    // Интерфейс событий (Шина данных)
    public interface IDeviceEvents
    {
        void Publish<T>(string deviceName, T data) where T : DeviceData;
    }
}
