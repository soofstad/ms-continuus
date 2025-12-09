using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSContinuus.Storage;

public interface IStorageClient
{
    public IEnumerable<string> UploadedObjects { get; }
    public Task EnsureContainer();
    public Task UploadArchive(string filePath);
    public Task DeleteArchivesBefore(DateTimeOffset before, string tag);

    public Task DeleteArchive(string archiveName);
}