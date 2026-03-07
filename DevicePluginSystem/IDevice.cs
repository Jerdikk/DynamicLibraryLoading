namespace DevicePluginSystem
{
    public interface IDevice
    {
        string Name { get; }
        void Connect();
    }
}
