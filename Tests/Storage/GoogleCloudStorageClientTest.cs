using Microsoft.Extensions.Configuration;
using MSContinuus;
using MSContinuus.Storage;

namespace Tests.Storage;

[TestFixture]
[TestOf(typeof(GoogleCloudStorageClient))]
public class GoogleCloudStorageClientTest
{
    [SetUp]
    public void SetUp()
    {
        var config = new Config(new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build());
        _storageClient = new GoogleCloudStorageClient(config);
    }

    [TearDown]
    public async Task TearDown()
    {
        var tasks = _storageClient.UploadedObjects.Select(archive => _storageClient.DeleteArchive(archive));
        await Task.WhenAll(tasks);
    }

    private GoogleCloudStorageClient _storageClient;

    [Test]
    public async Task UploadFile_ShouldNotThrow()
    {
        Assert.DoesNotThrowAsync(async () => await _storageClient.UploadArchive("test-data/2025__12__30__test.tar.gz"));
    }


    [Test]
    public async Task UploadArchive_ShouldThrowNotImplementedException_WhenFilePathIsValid()
    {
        var filePath = "invalid/path/to/file";
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await _storageClient.UploadArchive(filePath));
    }

    [Test]
    public async Task UploadArchive_ShouldThrowNotImplementedException_WhenFilePathIsNull()
    {
        string filePath = null;
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageClient.UploadArchive(filePath));
    }
}