using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codex.Host.Modules;

public class GitPoller(
    ModuleRegistry registry,
    ModuleBuilder builder,
    ModuleLoader loader,
    IServiceProvider hostSp,
    IConfiguration config,
    ILogger<GitPoller> logger) : BackgroundService
{
    private readonly string _modulesPath = config["Codex:ModulesPath"] ?? "modules";
    private readonly string _outputPath = config["Codex:ModulesOutputPath"] ?? "data/modules";
    private readonly int _intervalSeconds = int.Parse(config["Codex:PollerIntervalSeconds"] ?? "30");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GitPoller started. Watching: {Path}", Path.GetFullPath(_modulesPath));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PollAsync(); }
            catch (Exception ex) { logger.LogError(ex, "GitPoller tick failed"); }
            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }
    }

    private async Task PollAsync()
    {
        await GitPullAsync();

        var fullModulesPath = Path.GetFullPath(_modulesPath);
        if (!Directory.Exists(fullModulesPath)) return;

        var found = new HashSet<string>();
        foreach (var moduleDir in Directory.GetDirectories(fullModulesPath))
        {
            var manifestPath = Path.Combine(moduleDir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            ModuleManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ModuleManifest>(
                    File.ReadAllText(manifestPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse manifest at {Path}", manifestPath);
                continue;
            }

            if (manifest?.Id is null) continue;
            found.Add(manifest.Id);

            var hash = ComputeDirectoryHash(moduleDir);
            var existing = registry.Get(manifest.Id);

            if (existing is null)
            {
                registry.Register(new ModuleEntry
                {
                    Id = manifest.Id,
                    DisplayName = manifest.DisplayName ?? manifest.Id,
                    Version = manifest.Version ?? "0.0.0",
                    Description = manifest.Description,
                    Icon = manifest.Icon,
                    PageRoute = manifest.PageRoute ?? $"/{manifest.Id}",
                    ManifestHash = hash,
                    Status = ModuleStatus.Discovered
                });
                logger.LogInformation("Discovered new module: {Id}", manifest.Id);
                await BuildAndLoadAsync(manifest.Id, moduleDir);
            }
            else if (existing.ManifestHash != hash)
            {
                existing.ManifestHash = hash;
                existing.Version = manifest.Version ?? existing.Version;
                existing.DisplayName = manifest.DisplayName ?? existing.DisplayName;
                logger.LogInformation("Module changed: {Id}", manifest.Id);
                await BuildAndLoadAsync(manifest.Id, moduleDir);
            }
        }

        foreach (var entry in registry.All().Where(e => !found.Contains(e.Id)).ToList())
        {
            logger.LogInformation("Module removed from repo: {Id}", entry.Id);
            loader.Unload(entry.Id);
        }
    }

    private async Task BuildAndLoadAsync(string moduleId, string moduleDir)
    {
        registry.UpdateStatus(moduleId, ModuleStatus.Building);
        var (success, log, backendOut) = await builder.BuildAsync(moduleId, moduleDir, _outputPath);

        if (!success)
        {
            registry.UpdateStatus(moduleId, ModuleStatus.BuildFailed, log);
            return;
        }

        // Copy frontend output to wwwroot
        var frontendOut = Path.Combine(_outputPath, moduleId, "frontend");
        var wwwFrontend = Path.Combine("wwwroot", "assets", "modules", moduleId);
        if (Directory.Exists(frontendOut))
        {
            if (Directory.Exists(wwwFrontend)) Directory.Delete(wwwFrontend, true);
            CopyDirectory(frontendOut, wwwFrontend);
        }

        loader.TryLoad(moduleId, backendOut, hostSp);

        var entry = registry.Get(moduleId);
        if (entry?.Status == ModuleStatus.Loaded)
            entry.BuildLog = log;
    }

    private async Task GitPullAsync()
    {
        var gitDir = Path.Combine(Path.GetFullPath(_modulesPath), ".git");
        if (!Directory.Exists(gitDir)) return;
        var (code, output) = await RunAsync("git", "pull", Path.GetFullPath(_modulesPath));
        if (code == 0)
            logger.LogDebug("git pull: {Output}", output.Trim());
        else
            logger.LogWarning("git pull exited with {Code}: {Output}", code, output.Trim());
    }

    public static string ComputeDirectoryHash(string path)
    {
        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
        using var sha = SHA256.Create();
        foreach (var f in files)
        {
            var nameBytes = Encoding.UTF8.GetBytes(f.Replace(path, "", StringComparison.OrdinalIgnoreCase));
            sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
            var contentBytes = File.ReadAllBytes(f);
            sha.TransformBlock(contentBytes, 0, contentBytes.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    private static void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    private static async Task<(int Code, string Output)> RunAsync(string exe, string args, string workDir)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        var output = await proc.StandardOutput.ReadToEndAsync();
        var err = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (proc.ExitCode, output + err);
    }

    private record ModuleManifest(string? Id, string? DisplayName, string? Version, string? Description, string? Icon, string? PageRoute);
}
