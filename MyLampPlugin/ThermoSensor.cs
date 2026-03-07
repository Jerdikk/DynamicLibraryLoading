using DevicePluginSystem;
namespace MyLampPlugin
{
    public class ThermoSensor : IDevice
    {
        private readonly IDeviceEvents _eventBus;
        private readonly ILogger _logger;

        public string Name => "Датчик-Т100";

        // DI автоматически подставит зависимости из основного приложения
        public ThermoSensor(IDeviceEvents eventBus, ILogger logger)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        public void Connect()
        {
            _logger.Log($"{Name} инициализирован.");

            // Имитируем отправку данных
            Task.Run(async () => {
                await Task.Delay(2000);
                var data = new TemperatureData { Value = 24.5 };
                _eventBus.Publish(Name, data);
            });
        }
    }
}
