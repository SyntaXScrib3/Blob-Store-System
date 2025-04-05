using System.Text;
using BlobStoreSystem.Domain.Services;

namespace BlobStoreSystem.Tests.Infrastructure;

public class LocalFileSystemBlobStorageTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly IBlobStorageProvider _storage;

    public LocalFileSystemBlobStorageTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"BlobTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testBasePath);

        _storage = new LocalFileSystemBlobStorage(_testBasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    [Fact]
    public async Task UploadBlobAsync_ShouldCreateFileWithCorrectContent()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        var data = Encoding.UTF8.GetBytes("Hello Local FS!");
        using var ms = new MemoryStream(data);

        // Act
        await _storage.UploadBlobAsync(blobId, ms);

        // Assert
        var filePath = Path.Combine(_testBasePath, blobId.ToString());
        Assert.True(File.Exists(filePath), "Uploaded file does not exist on disk.");

        var fileContent = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(data, fileContent);
    }

    [Fact]
    public async Task DownloadBlobAsync_ShouldReturnStreamWithFileContent()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        var originalContent = Encoding.UTF8.GetBytes("Download test content");
        var filePath = Path.Combine(_testBasePath, blobId.ToString());
        await File.WriteAllBytesAsync(filePath, originalContent);

        // Act
        using var stream = await _storage.DownloadBlobAsync(blobId);

        // Assert
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var downloadedBytes = ms.ToArray();

        Assert.Equal(originalContent, downloadedBytes);
    }

    [Fact]
    public async Task DownloadBlobAsync_ShouldThrowFileNotFound_IfBlobDoesNotExist()
    {
        // Arrange
        var nonExistentBlobId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            using var stream = await _storage.DownloadBlobAsync(nonExistentBlobId);
        });
    }

    [Fact]
    public async Task DeleteBlobAsync_ShouldRemoveFile()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        var filePath = Path.Combine(_testBasePath, blobId.ToString());

        // Create a file
        await File.WriteAllTextAsync(filePath, "DeleteMe");

        // Act
        await _storage.DeleteBlobAsync(blobId);

        // Assert
        Assert.False(File.Exists(filePath), "File should have been deleted from disk.");
    }

    [Fact]
    public async Task DeleteBlobAsync_ShouldNotThrow_IfFileDoesNotExist()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        var filePath = Path.Combine(_testBasePath, blobId.ToString());

        // Ensure file doesn't exist
        if (File.Exists(filePath))
            File.Delete(filePath);

        // Act
        var ex = await Record.ExceptionAsync(() => _storage.DeleteBlobAsync(blobId));

        // Assert
        Assert.Null(ex); // No exception thrown
    }

    [Fact]
    public async Task UploadBlobAsync_ShouldOverwriteFile_WhenAlreadyExists()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        var filePath = Path.Combine(_testBasePath, blobId.ToString());

        // Pre-create the file with some content
        await File.WriteAllTextAsync(filePath, "Old Content");

        // Upload new content with the same blobId
        var newContent = Encoding.UTF8.GetBytes("New Overwritten Content");
        using var ms = new MemoryStream(newContent);

        // Act
        await _storage.UploadBlobAsync(blobId, ms);

        // Assert
        var finalContent = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(newContent, finalContent);
    }
}
