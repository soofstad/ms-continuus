using MSContinuus;
using MSContinuus.Types;

namespace Tests.Mocks;

public class MockIGithubApi : IGithubApi
{
    public Task<List<string>> ListRepositories()
    {
        return Task.FromResult(new List<string> { "repo-alpha", "repo-beta", "repo-gamma" });
    }

    public Task<List<Migration>> ListMigrations()
    {
        var now = DateTime.Now;
        return Task.FromResult(new List<Migration>
        {
            new(1, Guid.Empty.ToString(), "Exported", now, ["repo-alpha"]),
            new(2, Guid.Empty.ToString(), "Pending", now, ["repo-beta"]),
            new(2, Guid.Empty.ToString(), "Failed", now, ["repo-gamma"])
        });
    }

    public Task<Migration> MigrationStatus(int migrationId)
    {
        return Task.FromResult(new Migration(migrationId, Guid.Empty.ToString(), "Exported", DateTime.Now, ["repo-alpha", "repo-beta"]));
    }

    public Task<string> DownloadArchive(int migrationId, int volume, List<string> repoList)
    {
        return Task.FromResult($"mock_archive_{migrationId}_vol{volume}.zip");
    }

    public Task<Migration> StartMigration(List<string> repositoryList)
    {
        return Task.FromResult(new Migration(1, Guid.Empty.ToString(), "Pending", DateTime.Now, ["repo-alpha", "repo-beta"]));
    }
}