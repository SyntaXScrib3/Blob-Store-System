namespace BlobStoreSystem.Domain.Services;

public interface IBlobStorageProvider
{
    Task UploadBlobAsync(Guid blobId, Stream data);
    Task<Stream> DownloadBlobAsync(Guid blobId);
    Task DeleteBlobAsync(Guid blobId);
}

