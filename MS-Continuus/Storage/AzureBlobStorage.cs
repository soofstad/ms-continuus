using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MSContinuus.Types;

namespace MSContinuus.Storage;

public class AzureBlobStorage(Config config) : IStorageClient
{
    private readonly BlobServiceClient _client = new(config.StorageAccountConnectionString);
    private BlobContainerClient _containerClient;

    public IEnumerable<string> UploadedObjects { get; }

    public async Task EnsureContainer()
    {
        try
        {
            Console.WriteLine($"Ensuring Blob container '{_client.Uri}/{config.StorageContainer}'");
            BlobContainerClient container = await _client.CreateBlobContainerAsync(config.StorageContainer);
            _containerClient = container;
        }
        catch (RequestFailedException error)
        {
            _containerClient = error.ErrorCode switch
            {
                "ContainerAlreadyExists" => _client.GetBlobContainerClient(config.StorageContainer),
                "InvalidResourceName" => throw new ArgumentException(
                    $"The specified resource name contains invalid characters. '{config.StorageContainer}'"
                ),
                _ => _containerClient
            };
        }
        catch (Exception error)
        {
            Console.WriteLine(error.InnerException?.Message);
            Console.WriteLine(error.InnerException?.StackTrace);
            Environment.Exit(1);
        }
    }

    public async Task UploadArchive(string filePath)
    {
        var timeStarted = DateTime.Now;
        var fileName = Path.GetFileName(filePath)?.Replace("__", "/");
        var blobClient = _containerClient.GetBlobClient(fileName);
        var metadata = new Dictionary<string, string>();

        Console.WriteLine(
            "Uploading to Blob storage as:\n" +
            $"\t{config.StorageContainer}/{fileName}\n" +
            $"\tmetadata: {{ retention: {config.BlobTag} }}"
        );
        // TODO: Make sure hierarchy filename is correct (%2F becomes /?)
        await using var uploadFileStream = File.OpenRead(filePath);
        var fileSize = uploadFileStream.Length;
        Console.WriteLine($"\tsize: {Utility.BytesToString(fileSize)}");

        await blobClient.UploadAsync(uploadFileStream, true);
        uploadFileStream.Close();
        metadata["retention"] = config.BlobTag;
        await blobClient.SetMetadataAsync(metadata);
        Console.WriteLine($"\tAverage upload speed: {Utility.TransferSpeed(fileSize, timeStarted)}");
        Console.WriteLine("\tDeleting file from disk...");
        File.Delete(filePath);
    }

    // List every blob, if tag eq input tag, and CreatedOn is older than input date, delete it
    public async Task DeleteArchivesBefore(DateTimeOffset before, string tag)
    {
        var blobList = await ListBlobs();
        foreach (var archiveName in blobList.Where(b => b.RetentionClass == tag)
                     .Where(b => b.Created < before)
                     .Select(b => b.Name))
            DeleteArchive(archiveName);
    }

    Task IStorageClient.DeleteArchive(string archiveName)
    {
        throw new NotImplementedException();
    }

    private async Task<IEnumerable<Archive>> ListBlobs()
    {
        var blobList = new List<BlobItem>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync(BlobTraits.Metadata)) blobList.Add(blobItem);

        return blobList.Select(b =>
        {
            b.Metadata.TryGetValue("retention", out var retention);
            var createdOn = b.Properties.CreatedOn ?? DateTimeOffset.MinValue;
            return new Archive(b.Name, retention, createdOn);
        });
    }

    private void DeleteArchive(string fileName)
    {
        _containerClient.DeleteBlob(fileName);
        Console.WriteLine($"Deleted blob {fileName}");
    }
}