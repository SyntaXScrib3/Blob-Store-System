namespace BlobStoreSystem.Domain.Services;

public interface IFsProvider
{
    // 1) Directories
    Task CreateDirectoryAsync(string path);
    Task DeleteDirectoryAsync(string path);
    Task CopyDirectoryAsync(string path, string newPath);
    Task MoveDirectoryAsync(string path, string newPath);
    Task<IReadOnlyList<Entities.FsNode>> ListDirectoryAsync(string path);
    Task RenameDirectoryAsync(string oldPath, string newName);

    // 2) Files
    Task WriteFileAsync(string path, byte[] content);
    Task<byte[]> ReadFileAsync(string path);
    Task DeleteFileAsync(string path);
    Task CopyFileAsync(string path, string newPath);
    Task MoveFileAsync(string path, string newPath);
    Task RenameFileAsync(string oldPath, string newName);

    // 3) Info
    Task<Entities.FsNode?> GetInfoAsync(string path);

    // 4) Working directory
    Task SetWorkingDirectoryAsync(string path);
    Task<string> GetWorkingDirectoryAsync();
}
