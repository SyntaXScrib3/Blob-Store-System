namespace BlobStoreSystem.Domain.Services;

public class LocalFileSystemBlobStorage : IBlobStorageProvider
{
    private readonly string _basePath;

    public LocalFileSystemBlobStorage(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task UploadBlobAsync(Guid blobId, Stream data)
    {
        var filePath = Path.Combine(_basePath, blobId.ToString());
        using var fileStream = File.Create(filePath);
        await data.CopyToAsync(fileStream);
    }

    public async Task<Stream> DownloadBlobAsync(Guid blobId)
    {
        var filePath = Path.Combine(_basePath, blobId.ToString());
        return File.OpenRead(filePath);
    }

    public Task DeleteBlobAsync(Guid blobId)
    {
        var filePath = Path.Combine(_basePath, blobId.ToString());
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}