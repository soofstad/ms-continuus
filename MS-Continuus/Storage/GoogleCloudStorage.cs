using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using MSContinuus.Types;

namespace MSContinuus.Storage;

public class GoogleCloudStorageClient(Config config) : IStorageClient
{
    // TODO: read this from config
    private const string BucketName = GoogleCloudStorageConfig.BucketName;
    private readonly StorageClient _client = StorageClient.Create();
    private readonly List<string> _uploadedObjects = [];
    public IEnumerable<string> UploadedObjects => _uploadedObjects;

    public async Task EnsureContainer()
    {
        // Just check that the bucket exists and that we have access
        _ = await _client.GetBucketAsync(BucketName);
    }

    public async Task UploadArchive(string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath).Replace("__", "/");
        await _client.UploadObjectAsync(BucketName, fileName, "application/zip", fileStream);
        _uploadedObjects.Add(fileName);
    }

    public Task DeleteArchivesBefore(DateTimeOffset before, string tag)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteArchive(string archiveName)
    {
        var objectName = archiveName.Replace("__", "/");
        await _client.DeleteObjectAsync(BucketName, objectName, new DeleteObjectOptions());
    }
}