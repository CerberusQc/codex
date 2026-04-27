using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Codex.Host.Modules;

public class ModuleBuilder(ILogger<ModuleBuilder> logger)
{
    public async Task<(bool Success, string Log, string BackendOutputPath)> BuildAsync(
        string moduleId, string moduleSrcPath, string outputBasePath)
    {
        if (!Directory.Exists(moduleSrcPath))
            return (false, $"Module source path not found: {moduleSrcPath}", "");

        var log = new StringBuilder();
        var backendSrc = Path.Combine(moduleSrcPath, "backend");
        var frontendSrc = Path.Combine(moduleSrcPath, "frontend");
        var backendOut = Path.Combine(outputBasePath, moduleId, "backend");
        var frontendOut = Path.Combine(outputBasePath, moduleId, "frontend");

        Directory.CreateDirectory(backendOut);
        Directory.CreateDirectory(frontendOut);

        // Build backend
        log.AppendLine("=== Backend Build ===");
        if (!Directory.Exists(backendSrc))
        {
            log.AppendLine("No backend/ directory found.");
            return (false, log.ToString(), "");
        }

        var (backOk, backLog) = await RunProcessAsync("dotnet", $"build \"{backendSrc}\" -o \"{backendOut}\" --nologo", moduleSrcPath);
        log.AppendLine(backLog);
        if (!backOk)
        {
            logger.LogWarning("Backend build failed for {ModuleId}", moduleId);
            return (false, log.ToString(), "");
        }

        // Build frontend (optional)
        if (Directory.Exists(frontendSrc))
        {
            log.AppendLine("=== Frontend Build ===");
            var (npmOk, npmLog) = await RunProcessAsync("npm", "ci", frontendSrc);
            log.AppendLine(npmLog);
            if (npmOk)
            {
                var (vitOk, vitLog) = await RunProcessAsync("npm", $"run build -- --outDir \"{frontendOut}\"", frontendSrc);
                log.AppendLine(vitLog);
                if (!vitOk)
                {
                    logger.LogWarning("Frontend build failed for {ModuleId}", moduleId);
                    return (false, log.ToString(), "");
                }
            }
            else
            {
                logger.LogWarning("npm ci failed for {ModuleId}", moduleId);
                return (false, log.ToString(), "");
            }
        }

        logger.LogInformation("Built module {ModuleId}", moduleId);
        return (true, log.ToString(), backendOut);
    }

    private static async Task<(bool Ok, string Output)> RunProcessAsync(string exe, string args, string workDir)
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
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (proc.ExitCode == 0, stdout + (stderr.Length > 0 ? "\n" + stderr : ""));
    }
}
