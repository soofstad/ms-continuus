using Microsoft.Extensions.Configuration;
using MSContinuus;
using MSContinuus.Types;
using Tests.Mocks;

namespace Tests;

[TestFixture]
[TestOf(typeof(Archiver))]
public class ArchiverTest
{
    [SetUp]
    public void SetUp()
    {
        var config = new Config(new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build());
        _archiver = new Archiver(new MockStorageClient(), new MockIGithubApi(), config);
    }

    private Archiver _archiver;

    [Test]  // TODO: Make sure this test actually does something interesting
    public async Task BackupArchive_StartsMigrationsAndUploadsBlobs()
    {
        // Arrange
        var repositories = new List<string> { "repo1", "repo2", "repo3" };

        // Act
        await _archiver.BackupArchive();


    }

    [Test]
    public async Task BackupArchive_RetriesFailedMigrations()
    {
        // Arrange
        var repositories = new List<string> { "repo1", "repo2", "repo3" };
        var migration1 = new Migration(1, Guid.NewGuid().ToString(), "Exported", null, repositories.Take(2).ToList());
        var migration2 = new Migration(2, Guid.NewGuid().ToString(), "Failed", null, repositories.Skip(2).ToList());

        // Act
        await _archiver.BackupArchive();
    }

    // [Test]
    // public void BackupArchive_ThrowsExceptionWhenUploadFailsThreeTimes()
    // {
    //     // Arrange
    //     var repositories = new List<string> { "repo1" };
    //     var migration = new Migration(1, Guid.NewGuid().ToString(), "Exported", null, repositories);
    //
    //     _mockGithubApi.Setup(x => x.ListRepositories()).ReturnsAsync(repositories);
    //     _mockGithubApi.Setup(x => x.StartMigration(It.IsAny<List<string>>())).ReturnsAsync(migration);
    //     _mockGithubApi.Setup(x => x.MigrationStatus(It.IsAny<int>())).ReturnsAsync(migration);
    //     _mockGithubApi.Setup(x => x.DownloadArchive(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>()))
    //         .ReturnsAsync($"/path/to/migration_{migration.Id}.tar.gz");
    //
    //     _mockStorageClient.SetupSequence(x => x.UploadArchive(It.IsAny<string>()))
    //         .Throws<IOException>()
    //         .Throws<IOException>()
    //         .Throws<IOException>();
    //
    //     // Act & Assert
    //     Assert.ThrowsAsync<Exception>(async () => await _archiver.BackupArchive());
    // }
}