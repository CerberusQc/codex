using Codex.Host.Modules;

namespace Codex.Host.Tests.Modules;

public class GitPollerTests
{
    [Fact]
    public void ComputeHash_same_content_same_hash()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), "{\"id\":\"test\"}");

        var h1 = GitPoller.ComputeDirectoryHash(dir);
        var h2 = GitPoller.ComputeDirectoryHash(dir);
        Assert.Equal(h1, h2);

        Directory.Delete(dir, true);
    }

    [Fact]
    public void ComputeHash_different_content_different_hash()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), "{\"id\":\"test\"}");
        var h1 = GitPoller.ComputeDirectoryHash(dir);

        File.WriteAllText(Path.Combine(dir, "manifest.json"), "{\"id\":\"test\",\"version\":\"2.0\"}");
        var h2 = GitPoller.ComputeDirectoryHash(dir);
        Assert.NotEqual(h1, h2);

        Directory.Delete(dir, true);
    }

    [Fact]
    public void ComputeHash_new_file_changes_hash()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), "{\"id\":\"test\"}");
        var h1 = GitPoller.ComputeDirectoryHash(dir);

        File.WriteAllText(Path.Combine(dir, "newfile.txt"), "extra");
        var h2 = GitPoller.ComputeDirectoryHash(dir);
        Assert.NotEqual(h1, h2);

        Directory.Delete(dir, true);
    }
}
