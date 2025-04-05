using BlobStoreSystem.Domain.Services;
using System.Collections.Concurrent;

namespace BlobStoreSystem.Tests.Helpers;

public class InMemoryBlobStorage : IBlobStorageProvider
{
    private readonly ConcurrentDictionary<Guid, byte[]> _store = new ConcurrentDictionary<Guid, byte[]>();

    public Task UploadBlobAsync(Guid blobId, Stream data)
    {
        using var ms = new MemoryStream();
        data.CopyTo(ms);
        _store[blobId] = ms.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadBlobAsync(Guid blobId)
    {
        if (_store.TryGetValue(blobId, out var bytes))
        {
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }
        throw new FileNotFoundException($"Blob with ID {blobId} not found in storage.");
    }
    public Task DeleteBlobAsync(Guid blobId)
    {
        _store.TryRemove(blobId, out _);
        return Task.CompletedTask;
    }
}
