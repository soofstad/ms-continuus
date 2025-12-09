using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MSContinuus.Storage;
using MSContinuus.Types;

namespace MSContinuus;

public class Archiver(IStorageClient storageClient, IGithubApi githubApi, Config config)
{
    private readonly IGithubApi _githubApi = githubApi;
    private readonly IStorageClient _storageClient = storageClient;

    public async Task DeleteWeeklyBlobs(int days)
    {
        var olderThan = Utility.DateMinusDays(days);
        Console.WriteLine($"Deleting blobs with retention='weekly' older than {olderThan}");
        await _storageClient.DeleteArchivesBefore(olderThan, "weekly");
    }

    public async Task DeleteMonthlyBlobs()
    {
        var olderThan = Utility.DateMinusDays(config.MonthlyRetention);
        Console.WriteLine($"Deleting blobs with retention='monthly' older than {olderThan}");
        await _storageClient.DeleteArchivesBefore(olderThan, "monthly");
    }

    private async Task<bool> DownloadMigrationAndUploadBackup(Migration migration, int index)
    {
        var migStatus = await _githubApi.MigrationStatus(migration.Id);
        var exportTimer = 0;
        const int sleepIntervalSeconds = 60;
        while (migStatus.State != MigrationStatus.Exported)
        {
            await Task.Delay(sleepIntervalSeconds * 1_000);

            migStatus = await _githubApi.MigrationStatus(migStatus.Id);
            if (migStatus.State == MigrationStatus.Failed)
            {
                Console.WriteLine($"WARNING: Migration {migration.Id} failed... continuing with next");
                return false;
            }

            exportTimer++;
            Console.WriteLine(
                $"Waiting for {migStatus} to be ready... waited {exportTimer * sleepIntervalSeconds} seconds");
        }

        var archivePath = await _githubApi.DownloadArchive(migStatus.Id, index, migration.Repositories);
        await UploadArchiveWithRetry(archivePath);
        return true;
    }

    private async Task UploadArchiveWithRetry(string filePath)
    {
        var attempts = 0;
        while (attempts < 3)
        {
            try
            {
                await _storageClient.UploadArchive(filePath);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"WARNING: Failed to upload archive to blob storage ({e.Message}). Retrying in {Config.RetryUploadIntervalMs / 1000} seconds");
                Thread.Sleep(Config.RetryUploadIntervalMs);
            }

            attempts++;
        }

        throw new Exception($"Failed to upload blob '{filePath}' with {attempts} attempts.");
    }

    public async Task BackupArchive()
    {
        // Each migration can contain approx. 100~120 repositories
        // to keep the API from timing out. This also makes sense for retrying
        // smaller parts that failed in some way.

        // Use this for the mock API
        // const int chunkSize = 3;
        const int chunkSize = 100;

        var startedMigrations = new List<Migration>();
        var failedToMigrate = new Dictionary<int, (List<string>, int)>();
        var failedToMigrate2 = new Dictionary<int, (List<string>, int)>();

        Console.WriteLine("Fetching all repositories...");
        var allRepositoryList = await _githubApi.ListRepositories();

        var chunks = allRepositoryList.Count / chunkSize;
        var remainder = allRepositoryList.Count % chunkSize;

        Console.WriteLine(
            $"Starting migration of {allRepositoryList.Count} repositories divided into {chunks + 1} chunks");
        // Start the smallest migration first (remainder)
        startedMigrations.Add(
            await _githubApi.StartMigration(allRepositoryList.GetRange(chunks * chunkSize, remainder)));

        for (var i = 0; i < chunks; i++)
        {
            var chunkedRepositoryList = allRepositoryList.GetRange(i * chunkSize, chunkSize);
            try
            {
                startedMigrations.Add(await _githubApi.StartMigration(chunkedRepositoryList));
            }
            catch (HttpRequestException error)
            {
                Console.WriteLine($"WARNING: Failed to start migration...{error.Message}");
            }
        }

        // Iterate through all the started migrations, wait for them to complete,
        // download them, and upload them to blob-storage
        for (var i = 0; i < startedMigrations.Count; i++)
        {
            var migration = startedMigrations[i];
            var uploaded = await DownloadMigrationAndUploadBackup(migration, i);

            if (!uploaded) failedToMigrate[migration.Id] = (migration.Repositories, i);
        }

        // Go a second round to retry failed exports
        Console.WriteLine($"Retrying {failedToMigrate.Count} failed exports...");
        startedMigrations.Clear();
        foreach (var (id, (repos, volume)) in failedToMigrate)
            startedMigrations.Add(await _githubApi.StartMigration(repos));

        for (var i = 0; i < startedMigrations.Count; i++)
        {
            var migration = startedMigrations[i];
            // Grab original volume/chunk number based on index in the list.
            var volume = failedToMigrate.Values.ElementAt(i).Item2;
            var oldId = failedToMigrate.ElementAt(i).Key;
            var uploaded = await DownloadMigrationAndUploadBackup(migration, volume);

            if (!uploaded) failedToMigrate2[migration.Id] = (migration.Repositories, volume);
        }


        // Summary of failed migrations
        if (failedToMigrate2.Count > 0)
        {
            Console.WriteLine("WARNING: Some migration requests failed to migrate");
            foreach (var (id, (repos, volume)) in failedToMigrate2)
                Console.WriteLine($"\tMigration Id: {id}, Repositories: [{string.Join(",", repos)}]");
        }
        else
        {
            Console.WriteLine($"Successfully uploaded all archives of {allRepositoryList.Count} repositories");
        }
    }
}