using System.Collections.Generic;
using System.Threading.Tasks;
using MSContinuus.Types;

namespace MSContinuus;

public interface IGithubApi
{
    Task<List<string>> ListRepositories();
    Task<List<Migration>> ListMigrations();
    Task<Migration> MigrationStatus(int migrationId);
    Task<string> DownloadArchive(int migrationId, int volume, List<string> repoList);
    Task<Migration> StartMigration(List<string> repositoryList);
}