using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using DevicePluginSystem;

public class DeviceEventManager : IDeviceEvents
{
    public event Action<string, DeviceData> OnDataReceived;
    public void Publish<T>(string name, T data) where T : DeviceData => OnDataReceived?.Invoke(name, data);
}

class Program
{
    private static IServiceProvider _sp;
    private static string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    private static Dictionary<string, (DeviceLoadContext ctx, IDevice dev)> _plugins = new();

    static void Main()
    {
        // Настройка DI
        var events = new DeviceEventManager();
        events.OnDataReceived += (name, data) => {
            string val = data is TemperatureData t ? $"{t.Value}°C" : "Unknown";
            DateTime dateTime = DateTime.Now;
            Console.WriteLine($"{dateTime} [СОБЫТИЕ] {name} прислал данные: {val}");
        };

        _sp = new ServiceCollection()
            .AddSingleton<ILogger, ConsoleLogger>()
            .AddSingleton<IDeviceEvents>(events)
            .BuildServiceProvider();

        if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);

        Console.WriteLine(">>> Инициализация системы...");
        // 1. СКАНИРУЕМ ПАПКУ ПРИ СТАРТЕ
        ScanExistingPlugins();
        // 2. Выводим отчет о загруженных устройствах
        Console.WriteLine("\n========================================");
        if (_plugins.Count > 0)
        {
            Console.WriteLine($"Система готова. Загружено устройств: {_plugins.Count}");
            foreach (var entry in _plugins)
            {
                Console.WriteLine($" - {entry.Value.dev.Name} (файл: {Path.GetFileName(entry.Key)})");
            }
        }
        else
        {
            Console.WriteLine("Внимание: Устройства не найдены в папке /Plugins.");
        }
        Console.WriteLine("========================================\n");
        // Слежка за папкой
        using var watcher = new FileSystemWatcher(_path, "*.dll") { EnableRaisingEvents = true };
        watcher.Created += (s, e) => LoadPlugin(e.FullPath);
        watcher.Changed += (s, e) => LoadPlugin(e.FullPath);

        Console.WriteLine("Слежу за изменениями... Нажмите Enter для выхода.");
        Console.ReadLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LoadPlugin(string path)
    {
        string fileName = Path.GetFileName(path);

        // 1. Проверка: не пытаемся ли мы загрузить саму библиотеку интерфейсов?
        if (fileName.Equals("DeviceInterface.dll", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            // 2. Проверка: является ли файл валидной .NET сборкой?
            AssemblyName testName = AssemblyName.GetAssemblyName(path);

            var deviceLib = new DeviceLoadContext();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var devLibAssembly = deviceLib.LoadFromStream(fs);

            // 3. Ищем подходящие типы
            var validTypes = devLibAssembly.GetTypes().Where(t =>
                typeof(IDevice).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract).ToList();

            if (!validTypes.Any())
            {
                Console.WriteLine($"[Пропуск] {fileName}: Не содержит реализаций IDevice.");
                deviceLib.Unload();
                return;
            }

            foreach (var type in validTypes)
            {
                try
                {
                    // 4. Безопасное создание через DI с перехватом ошибок конструктора
                    var device = (IDevice)ActivatorUtilities.CreateInstance(_sp, type);

                    // Проверка совместимости версии (опционально)
                    if (device.Version != "1.0.0")
                    {
                        Console.WriteLine($"[Warn] {device.Name} имеет версию {device.Version}. Возможны ошибки.");
                    }

                    _plugins[path] = (deviceLib, device);
                    device.Connect();
                    Console.WriteLine($"[OK] Устройство загружено: {device.Name} ({device.Version})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] Не удалось активировать класс {type.Name} из {fileName}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }
        catch (BadImageFormatException)
        {
            Console.WriteLine($"[Ошибка] {fileName} не является валидной .NET библиотекой (возможно, x86/x64 конфликт).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Критическая ошибка] {fileName}: {ex.Message}");
        }
    }  // Новый вспомогательный метод
    static void ScanExistingPlugins()
    {
        var files = Directory.GetFiles(_path, "*.dll");
        foreach (var file in files)
        {
          //  Console.WriteLine($"[Старт] Обнаружен файл: {Path.GetFileName(file)}");
            LoadPlugin(file);
        }
    }
}
