using Codex.Host.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Core.Data;

public class CodexDbContext(DbContextOptions<CodexDbContext> options) : DbContext(options)
{
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<ModuleRegistration> ModuleRegistrations => Set<ModuleRegistration>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
}
