using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using DevicePluginSystem;

// Контекст для выгрузки DLL
public class DeviceLoadContext : AssemblyLoadContext
{
    public DeviceLoadContext() : base(isCollectible: true) { }
    protected override Assembly Load(AssemblyName n) => null;
}

// Реализация сервисов
public class ConsoleLogger : ILogger { public void Log(string m) => Console.WriteLine($"[LOG]: {m}"); }

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
            Console.WriteLine($"[СОБЫТИЕ] {name} прислал данные: {val}");
        };

        _sp = new ServiceCollection()
            .AddSingleton<ILogger, ConsoleLogger>()
            .AddSingleton<IDeviceEvents>(events)
            .BuildServiceProvider();

        if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);

        // 1. СКАНИРУЕМ ПАПКУ ПРИ СТАРТЕ
        ScanExistingPlugins();

        // Слежка за папкой
        using var watcher = new FileSystemWatcher(_path, "*.dll") { EnableRaisingEvents = true };
        watcher.Created += (s, e) => LoadPlugin(e.FullPath);
        watcher.Changed += (s, e) => LoadPlugin(e.FullPath);

        Console.WriteLine("Система готова. Нажмите Enter для выхода.");
        Console.ReadLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LoadPlugin(string path)
    {
        System.Threading.Thread.Sleep(500); // Пауза для освобождения файла процессом копирования

        if (_plugins.ContainsKey(path))
        {
            _plugins[path].ctx.Unload();
            _plugins.Remove(path);
            GC.Collect(); GC.WaitForPendingFinalizers();
            Console.WriteLine("Старая версия выгружена.");
        }

        try
        {
            var alc = new DeviceLoadContext();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var assembly = alc.LoadFromStream(fs);
            var type = assembly.GetTypes().FirstOrDefault(t => typeof(IDevice).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (type != null)
            {
                var device = (IDevice)ActivatorUtilities.CreateInstance(_sp, type);
                _plugins[path] = (alc, device);
                device.Connect();
                Console.WriteLine($"Загружен: {device.Name}");
            }
        }
        catch (Exception ex) { Console.WriteLine($"Ошибка: {ex.Message}"); }
    }
    // Новый вспомогательный метод
    static void ScanExistingPlugins()
    {
        var files = Directory.GetFiles(_path, "*.dll");
        foreach (var file in files)
        {
            Console.WriteLine($"[Старт] Обнаружен файл: {Path.GetFileName(file)}");
            LoadPlugin(file);
        }
    }
}
