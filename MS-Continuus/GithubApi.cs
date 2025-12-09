using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MSContinuus.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSContinuus;

public class GithubApi : IGithubApi
{
    private static readonly HttpClient Client = new();
    private readonly string _dateToday = $"{DateTime.Now:yyyy_MM_dd}";
    private readonly string _migrationsUrl;
    private readonly string _repoUrl;

    public GithubApi(Config config)
    {
        if (string.IsNullOrWhiteSpace(config.GithubToken))
            throw new ArgumentException(
                "GithubToken is empty. Make sure it's set in the environment variable 'GITHUB_TOKEN'");
        _migrationsUrl = config.Mode == Mode.Organization
            ? $"{config.GithubURL}/orgs/{config.Organization}/migrations"
            : $"{config.GithubURL}/user/migrations";
        _repoUrl = config.Mode == Mode.Organization
            ? $"{config.GithubURL}/orgs/{config.Organization}/repos"
            : $"{config.GithubURL}/users/{config.Organization}/repos";
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GithubToken}");
        Client.DefaultRequestHeaders.Add("User-Agent", "MSContinuus");
        Client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    public async Task<List<string>> ListRepositories()
    {
        var repoList = new List<string>();
        var page = 1;
        while (true)
        {
            var repos = await GetJsonArray($"{_repoUrl}?per_page=100&page={page}");
            if (repos == null) Environment.Exit(1);
            foreach (var jToken in repos)
            {
                var repo = (JObject)jToken;
                repoList.Add(repo["name"].ToString());
            }

            if (repos.Count < 100) break;
            page++;
        }

        return repoList;
    }

    public async Task<List<Migration>> ListMigrations()
    {
        var migrations = await GetJsonArray(_migrationsUrl);
        if (migrations == null) Environment.Exit(1);

        var migrationsList = new List<Migration>();
        foreach (var jToken in migrations)
        {
            var migration = (JObject)jToken;
            migrationsList.Add(new Migration(
                    int.Parse(migration["id"].ToString()),
                    migration["guid"].ToString(),
                    migration["state"].ToString(),
                    DateTime.Parse(migration["created_at"].ToString())
                )
            );
        }

        return migrationsList;
    }

    public async Task<Migration> MigrationStatus(int migrationId)
    {
        var migration = await GetJsonObject(_migrationsUrl + "/" + migrationId);
        if (migration == null) Environment.Exit(1);
        return new Migration(
            int.Parse(migration["id"].ToString()),
            migration["guid"].ToString(),
            migration["state"].ToString(),
            DateTime.Parse(migration["created_at"].ToString())
        );
    }

    public async Task<string> DownloadArchive(int migrationId, int volume, List<string> repoList)
    {
        var paddedVolume = volume.ToString();
        if (volume < 10) paddedVolume = "0" + paddedVolume;
        Directory.CreateDirectory("./tmp");
        var fileName =
            $"./tmp/{_dateToday}%2Fvol{paddedVolume}-{Utility.HashStingArray(repoList)}-{migrationId}.tar.gz";
        Console.WriteLine($"Downloading archive {migrationId}");
        var attempts = 1;
        const int retryInterval = 30_000;

        while (attempts < 5)
        {
            try
            {
                var timeStarted = DateTime.Now;
                var response = await Client.GetAsync(
                    $"{_migrationsUrl}/{migrationId.ToString()}/archive",
                    HttpCompletionOption.ResponseHeadersRead
                );
                response.EnsureSuccessStatusCode();
                var archiveSize = Utility.BytesToString(response.Content.Headers.ContentLength.GetValueOrDefault());
                Console.WriteLine($"\tSize of archive is {archiveSize}");
                await using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    await using (Stream streamToWriteTo = File.Open(fileName, FileMode.Create))
                    {
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    }
                }

                Console.WriteLine(
                    $"\tAverage download speed: {Utility.TransferSpeed(response.Content.Headers.ContentLength.GetValueOrDefault(), timeStarted)}"
                );
                Console.WriteLine($"Successfully downloaded archive to '{fileName}'");
                return fileName;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(
                    $"WARNING: Failed to download archive ({e.Message}). Retrying in {retryInterval / 1000} seconds"
                );
                Thread.Sleep(retryInterval);
            }

            attempts++;
        }

        throw new Exception($"Failed to download archive '{migrationId}' with {attempts} attempts.");
    }

    public async Task<Migration> StartMigration(List<string> repositoryList)
    {
        var payload = $"{{\"repositories\": {JsonConvert.SerializeObject(repositoryList)}}}";
        Console.WriteLine($"Starting migration with url: {_migrationsUrl}");
        var response = await Client.PostAsync(_migrationsUrl, new StringContent(payload));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var migration = JObject.Parse(content);

        var repoList = new List<string>();
        foreach (var jToken in migration["repositories"])
        {
            var repo = (JObject)jToken;
            repoList.Add(repo["name"].ToString());
        }

        var result = new Migration(
            int.Parse(migration["id"].ToString()),
            migration["guid"].ToString(),
            migration["state"].ToString(),
            DateTime.Parse(migration["created_at"].ToString()),
            repoList
        );
        Console.WriteLine($"\t{result}");
        return result;
    }

    private async Task<JObject> GetJsonObject(string url)
    {
        var responseBody = await Client.GetAsync(url);
        responseBody.EnsureSuccessStatusCode();
        var content = await responseBody.Content.ReadAsStringAsync();
        return JObject.Parse(content);
    }

    private async Task<JArray> GetJsonArray(string url)
    {
        Console.WriteLine($"Getting {url}...");
        var responseBody = await Client.GetAsync(url);
        responseBody.EnsureSuccessStatusCode();
        var content = await responseBody.Content.ReadAsStringAsync();
        return JArray.Parse(content);
    }
}