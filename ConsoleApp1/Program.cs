using DevicePluginSystem; // Ссылка на интерфейс
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
namespace ConsoleApp1
{


    class Program
    {
        /*
        static void Main()
        {
            /*   string pluginPath = AppContext.BaseDirectory+ @"MyLampPlugin.dll";

               // 1. Загружаем сборку
               Assembly assembly = Assembly.LoadFrom(pluginPath);

               // 2. Ищем типы, которые реализуют IDevice и не являются интерфейсами/абстрактными
               var deviceTypes = assembly.GetTypes()
                   .Where(t => typeof(IDevice).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

               foreach (var type in deviceTypes)
               {
                   // 3. Создаем экземпляр
                   IDevice device = (IDevice)Activator.CreateInstance(type);

                   Console.WriteLine($"Найдено устройство: {device.Name}");
                   device.Connect();
               }*/

        /*
        // 1. Указываем путь к папке с плагинами (например, папка "Plugins")
        string pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        // Создаем папку, если её нет
        if (!Directory.Exists(pluginsDirectory))
        {
            Directory.CreateDirectory(pluginsDirectory);
            Console.WriteLine($"Папка создана: {pluginsDirectory}. Положите туда DLL.");
            return;
        }

        // 2. Ищем все DLL файлы
        var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");
        var devices = new List<IDevice>();

        foreach (var file in dllFiles)
        {
            try
            {
                // Загружаем сборку
                Assembly assembly = Assembly.LoadFrom(file);

                // Ищем типы
                var types = assembly.GetTypes().Where(t =>
                    typeof(IDevice).IsAssignableFrom(t) &&
                    !t.IsInterface &&
                    !t.IsAbstract);

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IDevice device)
                    {
                        devices.Add(device);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // 3. Работаем со всеми найденными устройствами
        Console.WriteLine($"Найдено устройств: {devices.Count}");
        foreach (var device in devices)
        {
            Console.WriteLine($"--- {device.Name} ---");
            device.Connect();
        }
        */

        // Храним контекст и список устройств из этого контекста
        private static Dictionary<string, (DeviceLoadContext Context, List<IDevice> Devices)> _plugins = new();
        private static string _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        static void Main()
        {
            if (!Directory.Exists(_pluginsPath)) Directory.CreateDirectory(_pluginsPath);

            RefreshPlugins();

            using var watcher = new FileSystemWatcher(_pluginsPath, "*.dll");
            watcher.Changed += (s, e) => RefreshPlugins();
            watcher.Created += (s, e) => RefreshPlugins();
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Слежу за папкой Plugins. Нажмите Enter для выхода.");
            Console.ReadLine();
        }

        private static void RefreshPlugins()
        {
            // Небольшая пауза, чтобы файл успел скопироваться
            System.Threading.Thread.Sleep(500);

            var files = Directory.GetFiles(_pluginsPath, "*.dll");
            foreach (var file in files)
            {
                ReloadPlugin(file);
            }
        }

        // Метод помечен NoInlining, чтобы переменные внутри не удерживали сборку в памяти
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReloadPlugin(string path)
        {
            string fileName = Path.GetFileName(path);

            // 1. Если плагин уже загружен — выгружаем старую версию
            if (_plugins.ContainsKey(path))
            {
                Console.WriteLine($"[Выгрузка] {fileName}");
                var oldData = _plugins[path];
                oldData.Devices.Clear();
                oldData.Context.Unload(); // Помечаем на удаление из памяти
                _plugins.Remove(path);

                // Форсируем сборку мусора, чтобы файл реально освободился
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // 2. Загружаем новую версию
            try
            {
                var alc = new DeviceLoadContext();
                var devices = new List<IDevice>();

                // Загружаем через поток, чтобы не блокировать файл
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                Assembly assembly = alc.LoadFromStream(fs);

                var types = assembly.GetTypes().Where(t =>
                    typeof(IDevice).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IDevice device)
                    {
                        devices.Add(device);
                        Console.WriteLine($"[Загрузка] Найдено: {device.Name}");
                        device.Connect();
                    }
                }

                _plugins[path] = (alc, devices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ошибка] {fileName}: {ex.Message}");
            }


            Console.ReadLine();
        }
    }
    // Специальный контекст, который можно выгрузить
    public class DeviceLoadContext : AssemblyLoadContext
    {
        public DeviceLoadContext() : base(isCollectible: true) { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Позволяем загрузчику искать зависимости в папке приложения
            return null;
        }
    }
}
