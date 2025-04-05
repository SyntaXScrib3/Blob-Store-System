using BlobStoreSystem.Infrastructure.Data;
using BlobStoreSystem.Infrastructure.FileSystem;
using BlobStoreSystem.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BlobStoreSystem.Tests;

public class EfFsProviderTests
{
    private DbContextOptions<BlobStoreDbContext> CreateDbOptions()
            => new DbContextOptionsBuilder<BlobStoreDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

    private (BlobStoreDbContext, EfFsProvider) CreateProvider(Guid userId)
    {
        var options = CreateDbOptions();
        var dbContext = new BlobStoreDbContext(options);
        var blobStorage = new InMemoryBlobStorage();

        var provider = new EfFsProvider(dbContext, blobStorage, userId);
        return (dbContext, provider);
    }

    [Fact]
    public async Task CreateDirectoryAsync_ShouldCreateNewDirectory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);
        await fsProvider.CreateDirectoryAsync("/");

        // Act
        await fsProvider.CreateDirectoryAsync("/MyFolder");

        // Assert
        var dir = await db.Directories
            .FirstOrDefaultAsync(d => d.Path == "/MyFolder" && d.UserId == userId);
        Assert.NotNull(dir);
        Assert.Equal("MyFolder", dir.Name);
    }

    [Fact]
    public async Task CreateDirectoryAsync_ShouldThrowIfExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);
        await fsProvider.CreateDirectoryAsync("/");

        // First time creation
        await fsProvider.CreateDirectoryAsync("/FolderA");

        // Act & Assert: second time should fail
        await Assert.ThrowsAsync<InvalidOperationException>(()
            => fsProvider.CreateDirectoryAsync("/FolderA"));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_ShouldDeleteDirectoryAndChildren()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);
        await fsProvider.CreateDirectoryAsync("/");

        // Create a structure: /Parent /Parent/Child
        await fsProvider.CreateDirectoryAsync("/Parent");
        await fsProvider.CreateDirectoryAsync("/Parent/Child");
        // Create a file in /Parent
        await fsProvider.WriteFileAsync("/Parent/file.txt", Encoding.UTF8.GetBytes("Hello World!"));

        // Act
        await fsProvider.DeleteDirectoryAsync("/Parent");

        // Assert
        // The entire directory tree & files should be removed
        var parent = await db.Directories
            .FirstOrDefaultAsync(d => d.Path == "/Parent" && d.UserId == userId);
        Assert.Null(parent);

        var child = await db.Directories
            .FirstOrDefaultAsync(d => d.Path == "/Parent/Child" && d.UserId == userId);
        Assert.Null(child);

        var file = await db.Files
            .FirstOrDefaultAsync(f => f.Path == "/Parent/file.txt" && f.UserId == userId);
        Assert.Null(file);
    }

    [Fact]
    public async Task ListDirectoryAsync_ShouldReturnImmediateChildren()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        await fsProvider.CreateDirectoryAsync("/SubDir");
        await fsProvider.WriteFileAsync("/file1.txt", Encoding.UTF8.GetBytes("Hello From File 1"));
        await fsProvider.WriteFileAsync("/file2.txt", Encoding.UTF8.GetBytes("Hello From File 2"));

        // Act
        var items = await fsProvider.ListDirectoryAsync("/");

        // Assert
        // We expect 3 children: SubDir, file1.txt, file2.txt
        Assert.Equal(3, items.Count);
        var names = items.Select(i => i.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "file1.txt", "file2.txt", "SubDir" }, names);
    }

    [Fact]
    public async Task WriteFileAsync_ShouldReuseBlob_WhenSameContent()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        var content = Encoding.UTF8.GetBytes("IdenticalContent");

        // Write file1
        await fsProvider.WriteFileAsync("/file1.txt", content);
        // Write file2 with same content
        await fsProvider.WriteFileAsync("/file2.txt", content);

        var file1 = await db.Files.FirstAsync(f => f.Path == "/file1.txt");
        var file2 = await db.Files.FirstAsync(f => f.Path == "/file2.txt");

        Assert.Equal(file1.BlobId, file2.BlobId);

        // ReferenceCount on that blob should be 2
        var blob = await db.Blobs.FindAsync(file1.BlobId);
        Assert.Equal(2, blob!.ReferenceCount);
    }

    [Fact]
    public async Task ReadFileAsync_ShouldReturnFileContent()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        var content = Encoding.UTF8.GetBytes("ReadTest");
        await fsProvider.WriteFileAsync("/readme.txt", content);

        // Act
        var result = await fsProvider.ReadFileAsync("/readme.txt");

        //Assert
        Assert.Equal("ReadTest", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile_AndDecrementBlobRefCount()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        var content = Encoding.UTF8.GetBytes("DeleteMe");
        await fsProvider.WriteFileAsync("/deleteme.txt", content);

        var file = await db.Files
            .FirstOrDefaultAsync(f => f.Path == "/deleteme.txt");
        Assert.NotNull(file);

        var blob = await db.Blobs.FindAsync(file!.BlobId);
        Assert.Equal(1, blob!.ReferenceCount);

        // Act
        await fsProvider.DeleteFileAsync("/deleteme.txt");

        // Assert: file record should be removed
        var checkFile = await db.Files
            .FirstOrDefaultAsync(f => f.Path == "/deleteme.txt");
        Assert.Null(checkFile);

        // Blob should be removed as reference count goes to 0
        var checkBlob = await db.Blobs.FindAsync(blob.Id);
        Assert.Null(checkBlob);
    }

    [Fact]
    public async Task CopyFileAsync_ShouldIncrementRefCount_AndCreateNewFileRecord()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        var content = Encoding.UTF8.GetBytes("CopyFileTest");
        await fsProvider.WriteFileAsync("/fileA.txt", content);

        // Act
        await fsProvider.CopyFileAsync("/fileA.txt", "/fileB.txt");

        // Assert
        var fileA = await db.Files
            .FirstOrDefaultAsync(f => f.Path == "/fileA.txt");
        var fileB = await db.Files
            .FirstOrDefaultAsync(f => f.Path == "/fileB.txt");

        Assert.NotNull(fileA);
        Assert.NotNull(fileB);
        Assert.NotEqual(fileA!.Id, fileB!.Id);
        // The same blob should be referenced
        Assert.Equal(fileA.BlobId, fileB.BlobId);

        var blob = await db.Blobs.FindAsync(fileA.BlobId);
        Assert.Equal(2, blob!.ReferenceCount);
    }

    [Fact]
    public async Task MoveFileAsync_ShouldUpdatePath_AndKeepSameBlob()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        await fsProvider.CreateDirectoryAsync("/FolderA");
        await fsProvider.CreateDirectoryAsync("/FolderB");

        var content = Encoding.UTF8.GetBytes("MoveTest");
        await fsProvider.WriteFileAsync("/FolderA/moveMe.txt", content);

        // Act
        await fsProvider.MoveFileAsync("/FolderA/moveMe.txt", "/FolderB/moveMe.txt");

        // Assert
        var oldFile = await db.Files.FirstOrDefaultAsync(f => f.Path == "/FolderA/moveMe.txt");
        Assert.Null(oldFile);  // should be updated

        var newFile = await db.Files.FirstOrDefaultAsync(f => f.Path == "/FolderB/moveMe.txt");
        Assert.NotNull(newFile);

        // Blob ref count should remain 1
        var blob = await db.Blobs.FindAsync(newFile!.BlobId);
        Assert.NotNull(blob);
        Assert.Equal(1, blob!.ReferenceCount);
    }

    [Fact]
    public async Task MoveDirectoryAsync_ShouldRenameDir_AndUpdateChildrenPaths()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);
        await fsProvider.CreateDirectoryAsync("/");

        await fsProvider.CreateDirectoryAsync("/Old");
        await fsProvider.CreateDirectoryAsync("/Old/Sub");
        await fsProvider.WriteFileAsync("/Old/file1.txt", Encoding.UTF8.GetBytes("Test"));

        // Act
        await fsProvider.MoveDirectoryAsync("/Old", "/New");

        // Assert
        // Old directory shouldn't exist
        var oldDir = await db.Directories.FirstOrDefaultAsync(d => d.Path == "/Old");
        Assert.Null(oldDir);

        // New directory should exist
        var newDir = await db.Directories.FirstOrDefaultAsync(d => d.Path == "/New");
        Assert.NotNull(newDir);

        // Sub dir path updated
        var subDir = await db.Directories.FirstOrDefaultAsync(d => d.Path == "/New/Sub");
        Assert.NotNull(subDir);

        // File path updated
        var file = await db.Files.FirstOrDefaultAsync(f => f.Path == "/New/file1.txt");
        Assert.NotNull(file);
    }

    [Fact]
    public async Task GetInfoAsync_ShouldReturnDirectoryOrFile()
    {
        var userId = Guid.NewGuid();
        var (db, fsProvider) = CreateProvider(userId);

        await fsProvider.CreateDirectoryAsync("/");
        await fsProvider.WriteFileAsync("/fileX.bin", new byte[] { 1, 2, 3 });

        var infoDir = await fsProvider.GetInfoAsync("/");
        Assert.NotNull(infoDir);
        Assert.True(infoDir!.IsDirectory);

        var infoFile = await fsProvider.GetInfoAsync("/fileX.bin");
        Assert.NotNull(infoFile);
        Assert.False(infoFile!.IsDirectory);

        var infoNull = await fsProvider.GetInfoAsync("/NotExist");
        Assert.Null(infoNull);
    }

    [Fact]
    public async Task SetWorkingDirectory_ShouldAffectRelativePaths()
    {
        var userId = Guid.NewGuid();
        var (_, fsProvider) = CreateProvider(userId);
        await fsProvider.CreateDirectoryAsync("/");

        await fsProvider.CreateDirectoryAsync("/TopLevel");
        await fsProvider.SetWorkingDirectoryAsync("/TopLevel");
        await fsProvider.CreateDirectoryAsync("SecondLevel");
        // (relative path => "/TopLevel/SecondLevel")

        var listed = await fsProvider.ListDirectoryAsync("/");
        // We expect "TopLevel" in the root
        Assert.Single(listed);
        Assert.Equal("TopLevel", listed.First().Name);

        var subListed = await fsProvider.ListDirectoryAsync("/TopLevel");
        Assert.Single(subListed);
        Assert.Equal("SecondLevel", subListed.First().Name);
    }
}
