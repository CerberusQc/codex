using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Codex.Host.Core.Data;

public class CodexDbContextFactory : IDesignTimeDbContextFactory<CodexDbContext>
{
    public CodexDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<CodexDbContext>()
            .UseSqlite("Data Source=data/codex.db")
            .Options;
        return new CodexDbContext(opts);
    }
}
