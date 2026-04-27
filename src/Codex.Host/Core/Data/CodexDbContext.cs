using Codex.Host.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Core.Data;

public class CodexDbContext(DbContextOptions<CodexDbContext> options) : DbContext(options)
{
    public DbSet<Dashboard> Dashboards { get; set; } = null!;
    public DbSet<ModuleRegistration> ModuleRegistrations { get; set; } = null!;
    public DbSet<DataSource> DataSources { get; set; } = null!;
}
