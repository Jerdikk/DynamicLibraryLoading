using DevicePluginSystem;
// Реализация сервисов
public class ConsoleLogger : ILogger
{
    public void Log(string m)
    {
        DateTime dateTime = DateTime.Now;
        Console.WriteLine($"{dateTime} [LOG]: {m}");
    }
}
public class FileLogger : ILogger
{

    public void Log(string msg)
    {
        throw new NotImplementedException();
    }
}
