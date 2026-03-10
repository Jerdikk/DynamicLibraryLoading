using System.Reflection;
using System.Runtime.Loader;
// Контекст для выгрузки DLL
public class DeviceLoadContext : AssemblyLoadContext
{
    public DeviceLoadContext() : base(isCollectible: true) 
    {

    }
    protected override Assembly Load(AssemblyName n) => null;
}
