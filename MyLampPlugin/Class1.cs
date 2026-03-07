using DevicePluginSystem;
namespace MyLampPlugin
{
    public class Lamp : IDevice
    {
        public string Name => "Умная Лампа";
        public void Connect() => Console.WriteLine("Лампа подключена по Zigbee!");
    }
}
