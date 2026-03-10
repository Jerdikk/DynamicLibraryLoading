namespace DevicePluginSystem
{

    // Главный интерфейс устройства
    public interface IDevice
    {
        string Name { get; }
        string Version => "1.0.0"; // Значение по умолчанию
        void Connect();
    }
}
