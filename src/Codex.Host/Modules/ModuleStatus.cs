namespace Codex.Host.Modules;

public enum ModuleStatus
{
    Discovered,
    Building,
    BuildFailed,
    Loaded,
    LoadFailed,
    MissingDatasource,
    Unloading
}
