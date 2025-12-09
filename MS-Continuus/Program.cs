using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MSContinuus.Storage;
using MSContinuus.Types;

namespace MSContinuus;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var config = new Config(new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build());

        IStorageClient storageClient = config.StorageProvider switch
        {
            StorageProvider.AzureBlobStorage => new AzureBlobStorage(config),
            StorageProvider.GoogleCloudStorage => new GoogleCloudStorageClient(config),
            _ => throw new ArgumentOutOfRangeException($"Invalid storage provider: {config.StorageProvider}")
        };

        Console.WriteLine(config.GithubToken);
        Utility.PrintVersion();
        Console.WriteLine(config.ToString());
        Console.WriteLine("Starting backup of Github organization");
        var startTime = DateTime.Now;
        await storageClient.EnsureContainer();
        var archiver = new Archiver(storageClient, new GithubApi(config), config);
        await archiver.BackupArchive();
        await archiver.DeleteWeeklyBlobs(config.WeeklyRetention);
        await archiver.DeleteMonthlyBlobs();
        Console.WriteLine(
            $"MS-Continuus run complete. Started at {startTime}, finished at {DateTime.Now}, total run time: {DateTime.Now - startTime}");
    }
}