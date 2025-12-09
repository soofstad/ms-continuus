using Microsoft.Extensions.Configuration;
using MSContinuus.Types;

namespace MSContinuus;

public class Config(IConfiguration configuration)
{
    public const int RetryUploadIntervalMs = 30_000;
    public readonly string BlobTag = configuration.GetValue("BlobTag", "weekly");
    public readonly string GithubToken = configuration.GetValue<string>("GITHUB_TOKEN");
    public readonly string GithubURL = configuration.GetValue("GithubUrl", "https://api.github.com");

    public readonly Mode Mode = configuration.GetValue("Mode", Mode.Organization);
    public readonly int MonthlyRetention = configuration.GetValue("MonthlyRetention", 230);
    public readonly string Organization = configuration.GetValue<string>("GithubOrg");

    public readonly string StorageAccountConnectionString =
        configuration.GetValue<string>("StorageAccountConnectionString");

    // public readonly string GoogleCloudStorageBucket = configuration.GetValue("GoogleCloudStorageBucket", "github-backup-bucket");
    public readonly string StorageContainer = configuration.GetValue("BlobContainer", "github-archives");

    public readonly StorageProvider StorageProvider =
        configuration.GetValue("StorageProvider", StorageProvider.GoogleCloudStorage);

    public readonly int WeeklyRetention = configuration.GetValue("WeeklyRetention", 60);
    public readonly int YearlyRetention = configuration.GetValue("YearlyRetention", 420);


    public override string ToString()
    {
        var ghUrl = $"\n\tGITHUB URL: {GithubURL}";
        var org = $"\n\tORGANIZATION: {Organization}";
        var container = $"\n\tBLOB_CONTAINER: {StorageContainer}";
        var tag = $"\n\tBLOB_TAG: {BlobTag}";
        var weekRet = $"\n\tWEEKLY_RETENTION: {WeeklyRetention}";
        var monthRet = $"\n\tMONTHLY_RETENTION: {MonthlyRetention}";
        var yearRet = $"\n\tYEARLY_RETENTION: {YearlyRetention}";

        return "Configuration settings:" + ghUrl + org + container + tag + weekRet + monthRet + yearRet;
    }
}