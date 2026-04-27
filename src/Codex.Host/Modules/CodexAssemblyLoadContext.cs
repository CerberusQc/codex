using System.Reflection;
using System.Runtime.Loader;

namespace Codex.Host.Modules;

public class CodexAssemblyLoadContext : AssemblyLoadContext, IDisposable
{
    private readonly string _modulePath;

    public CodexAssemblyLoadContext(string moduleId, string modulePath)
        : base(moduleId, isCollectible: true)
    {
        _modulePath = modulePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = Path.Combine(_modulePath, $"{assemblyName.Name}.dll");
        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }

    public void Dispose() => Unload();
}
