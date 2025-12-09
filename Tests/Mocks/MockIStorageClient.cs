using MSContinuus.Storage;

namespace Tests.Mocks;

public class MockStorageClient : IStorageClient
{
    // In-memory storage to track "uploaded" files
    private readonly List<string> _uploadedFiles = new();

    // Returns the current list of files in our mock storage
    public IEnumerable<string> UploadedObjects => _uploadedFiles;

    public Task EnsureContainer()
    {
        Console.WriteLine("Mock: Container ensured.");
        return Task.CompletedTask;
    }

    public Task UploadArchive(string filePath)
    {
        // Simulate extracting the file name from the full path
        var fileName = Path.GetFileName(filePath);

        if (!_uploadedFiles.Contains(fileName))
        {
            _uploadedFiles.Add(fileName);
            Console.WriteLine($"Mock: Uploaded file '{fileName}'");
        }

        return Task.CompletedTask;
    }

    public Task DeleteArchivesBefore(DateTimeOffset before, string tag)
    {
        // In a simple mock that only stores string filenames, we lack the metadata 
        // (timestamps, tags) to accurately filter files here. 
        // You could extend this class to store a complex object with this data if needed.
        Console.WriteLine(
            $"Mock: Requested deletion of archives with tag '{tag}' before {before}. (No action taken in basic mock)");

        return Task.CompletedTask;
    }

    public Task DeleteArchive(string archiveName)
    {
        if (_uploadedFiles.Contains(archiveName))
        {
            _uploadedFiles.Remove(archiveName);
            Console.WriteLine($"Mock: Deleted archive '{archiveName}'");
        }
        else
        {
            Console.WriteLine($"Mock: Archive '{archiveName}' not found, nothing to delete.");
        }

        return Task.CompletedTask;
    }
}