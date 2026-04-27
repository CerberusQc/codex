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
        // Return null for shared SDK assembly so the host's already-loaded copy is used.
        // If we load our own copy, ICodexModule from the module ALC and from the host are
        // different type objects and IsAssignableFrom returns false.
        if (assemblyName.Name == "Codex.ModuleSDK")
            return null;

        var path = Path.Combine(_modulePath, $"{assemblyName.Name}.dll");
        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }

    public void Dispose() => Unload();
}
