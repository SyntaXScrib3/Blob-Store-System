using BlobStoreSystem.Domain.Entities;
using BlobStoreSystem.Domain.Services;
using BlobStoreSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlobStoreSystem.Infrastructure.FileSystem;
public class EfFsProvider : IFsProvider
{
    private readonly BlobStoreDbContext _dbContext;
    private readonly IBlobStorageProvider _blobStorage;

    private string _workingDirectory = "/";
    private readonly Guid _currentUserId;

    private static readonly Dictionary<string, string> ExtensionToMimeType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".md",  "text/markdown" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg","image/jpeg" },
        { ".gif", "image/gif" },
        { ".pdf", "application/pdf" }
    };

    public EfFsProvider(
        BlobStoreDbContext dbContext,
        IBlobStorageProvider blobStorage,
        Guid currentUserId
    )
    {
        _dbContext = dbContext;
        _blobStorage = blobStorage;
        _currentUserId = currentUserId;
    }

    #region IFsProvider Implementation

    public async Task CreateDirectoryAsync(string path)
    {
        // 1. Resolve the absolute path
        var fullPath = ResolvePath(path);

        // 2. Check if directory already exists
        var exists = await _dbContext.Directories
            .AnyAsync(d => d.Path == fullPath && d.UserId == _currentUserId);

        if (exists)
            throw new InvalidOperationException($"Directory '{fullPath}' already exists.");

        // 3. Figure out parent directory
        var (parentPath, dirName) = SplitPath(fullPath);
        DirectoryNode? parent = null;

        if (!string.IsNullOrEmpty(parentPath))
        {
            parent = await _dbContext.Directories
                .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);

            if (parent == null)
                throw new InvalidOperationException($"Parent directory '{parentPath}' does not exist.");
        }

        // 4. Create the directory node
        var newDir = new DirectoryNode
        {
            Name = dirName,
            Path = fullPath,
            ParentDirectoryId = parent?.Id,
            UserId = _currentUserId,
            Size = 0,
            MimeType = "inode/directory",
            CreatedBy = _currentUserId.ToString(),
            UpdatedBy = _currentUserId.ToString()
        };

        await _dbContext.Directories.AddAsync(newDir);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteDirectoryAsync(string path)
    {
        var fullPath = ResolvePath(path);

        // 1. Find the directory
        var dir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == fullPath && d.UserId == _currentUserId);

        if (dir == null)
            throw new InvalidOperationException($"Directory '{fullPath}' not found.");

        // 2. Recursively delete subdirectories & files
        await DeleteDirectoryRecursive(dir);

        await _dbContext.SaveChangesAsync();
    }

    public async Task CopyDirectoryAsync(string path, string newPath)
    {
        var sourcePath = ResolvePath(path);
        var targetPath = ResolvePath(newPath);

        if (targetPath == sourcePath)
            throw new InvalidOperationException("Cannot copy a directory to the same location.");

        if (targetPath.StartsWith(sourcePath + "/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot copy a directory into itself or a subfolder of itself.");


        var sourceDir = await _dbContext.Directories
            .Include(d => d.ParentDirectory)
            .FirstOrDefaultAsync(d => d.Path == sourcePath && d.UserId == _currentUserId);
        
        if (sourceDir == null)
            throw new InvalidOperationException($"Source directory '{sourcePath}' not found.");

        await CopyDirectoryRecursive(sourceDir, sourcePath, targetPath);

        await _dbContext.SaveChangesAsync();
    }

    public async Task MoveDirectoryAsync(string path, string newPath)
    {
        var sourcePath = ResolvePath(path);
        var targetPath = ResolvePath(newPath);

        if (targetPath == sourcePath)
            throw new InvalidOperationException("Cannot move a directory to the same location.");

        if (targetPath.StartsWith(sourcePath + "/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot move a directory into itself or a subfolder of itself.");


        var dir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == sourcePath && d.UserId == _currentUserId);

        if (dir == null)
            throw new InvalidOperationException($"Directory '{sourcePath}' not found.");

        // Update the path
        var (parentPath, dirName) = SplitPath(targetPath);
        dir.Name = dirName;
        dir.Path = targetPath;
        dir.UpdatedBy = _currentUserId.ToString();
        dir.UpdateDate = DateTime.UtcNow;

        // Update parent
        if (!string.IsNullOrEmpty(parentPath))
        {
            var newParent = await _dbContext.Directories
                .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);
            if (newParent == null)
                throw new InvalidOperationException($"Parent directory '{parentPath}' does not exist.");

            dir.ParentDirectoryId = newParent.Id;
        }
        else
        {
            dir.ParentDirectoryId = null;
        }

        // update paths of subdirectories and files
        await UpdateChildrenPathsRecursive(dir, sourcePath, targetPath);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<FsNode>> ListDirectoryAsync(string path)
    {
        var fullPath = ResolvePath(path);

        // Find directory
        var dir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == fullPath && d.UserId == _currentUserId);

        if (dir == null)
            throw new InvalidOperationException($"Directory '{fullPath}' not found.");

        // List immediate children
        // Files
        var files = await _dbContext.Files
            .Where(f => f.ParentDirectoryId == dir.Id && f.UserId == _currentUserId)
            .ToListAsync<FsNode>();

        // Directories
        var directories = await _dbContext.Directories
            .Where(sub => sub.ParentDirectoryId == dir.Id && sub.UserId == _currentUserId)
            .ToListAsync<FsNode>();

        return directories.Concat(files).ToList();
    }

    public async Task WriteFileAsync(string path, byte[] content)
    {
        if (content == null)
        {
            content = Array.Empty<byte>();
        }

        var fullPath = ResolvePath(path);
        var (parentPath, fileName) = SplitPath(fullPath);

        // 1. Ensure parent directory exists
        var parent = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);

        if (parent == null)
            throw new InvalidOperationException($"Parent directory '{parentPath}' does not exist.");

        // 2. Check if file already exists
        var existingFile = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == fullPath && f.UserId == _currentUserId);

        // 3. Deduplication: compute hash (SHA-256)
        var hash = ComputeSHA256(content);
        var blob = await _dbContext.Blobs.FirstOrDefaultAsync(b => b.Hash == hash);

        if (blob == null)
        {
            blob = new Blob
            {
                Hash = hash,
                Size = content.LongLength,
                ReferenceCount = 0,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            };
            await _dbContext.Blobs.AddAsync(blob);
        }

        // Increment reference count
        blob.ReferenceCount++;
        blob.UpdateDate = DateTime.UtcNow;

        // 4. Store content in the blob storage
        using var ms = new MemoryStream(content);
        await _blobStorage.UploadBlobAsync(blob.Id, ms);

        // 5. Detect MIME type from file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string mimeType = ExtensionToMimeType.TryGetValue(extension, out var knownType)
            ? knownType
            : "application/octet-stream";

        if (existingFile == null)
        {
            // Create a new FileNode
            var newFile = new FileNode
            {
                Name = fileName,
                Path = fullPath,
                ParentDirectoryId = parent.Id,
                BlobId = blob.Id,
                Size = content.LongLength,
                MimeType = mimeType,
                CreatedBy = _currentUserId.ToString(),
                UpdatedBy = _currentUserId.ToString(),
                UserId = _currentUserId
            };

            await _dbContext.Files.AddAsync(newFile);
        }
        else
        {
            var oldBlob = await _dbContext.Blobs.FindAsync(existingFile.BlobId);
            if (oldBlob != null)
            {
                oldBlob.ReferenceCount--;
                oldBlob.UpdateDate = DateTime.UtcNow;
            }

            existingFile.BlobId = blob.Id;
            existingFile.Size = content.LongLength;
            existingFile.UpdateDate = DateTime.UtcNow;
            existingFile.UpdatedBy = _currentUserId.ToString();
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<byte[]> ReadFileAsync(string path)
    {
        var fullPath = ResolvePath(path);

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == fullPath && f.UserId == _currentUserId);

        if (file == null)
            throw new InvalidOperationException($"File '{fullPath}' not found.");

        var blob = await _dbContext.Blobs.FindAsync(file.BlobId);
        if (blob == null)
            throw new InvalidOperationException("Blob record not found. Database might be inconsistent.");

        using var stream = await _blobStorage.DownloadBlobAsync(blob.Id);
        using var ms = new MemoryStream();
        
        await stream.CopyToAsync(ms);
        
        return ms.ToArray();
    }

    public async Task DeleteFileAsync(string path)
    {
        var fullPath = ResolvePath(path);

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == fullPath && f.UserId == _currentUserId);

        if (file == null)
            throw new InvalidOperationException($"File '{fullPath}' not found.");

        // Decrement reference count in Blob
        var blob = await _dbContext.Blobs.FindAsync(file.BlobId);
        if (blob != null)
        {
            blob.ReferenceCount--;
            blob.UpdateDate = DateTime.UtcNow;
        }

        _dbContext.Files.Remove(file);
        await _dbContext.SaveChangesAsync();

        // if reference count hits 0, remove the blob
        if (blob != null && blob.ReferenceCount <= 0)
        {
            _dbContext.Blobs.Remove(blob);
            await _dbContext.SaveChangesAsync();

            // Also delete from physical storage
            await _blobStorage.DeleteBlobAsync(blob.Id);
        }
    }

    public async Task CopyFileAsync(string path, string newPath)
    {
        var sourcePath = ResolvePath(path);
        var targetPath = ResolvePath(newPath);

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == sourcePath && f.UserId == _currentUserId);
        
        if (file == null)
            throw new InvalidOperationException($"Source file '{sourcePath}' not found.");

        var (parentPath, fileName) = SplitPath(targetPath);
        
        var parentDir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);
        
        if (parentDir == null)
            throw new InvalidOperationException($"Target directory '{parentPath}' does not exist.");

        // Dedup approach: the same Blob can be reused
        var blob = await _dbContext.Blobs.FindAsync(file.BlobId);
        
        if (blob == null)
            throw new InvalidOperationException("Blob not found. Database might be inconsistent.");

        blob.ReferenceCount++;
        blob.UpdateDate = DateTime.UtcNow;

        var newFile = new FileNode
        {
            Name = fileName,
            Path = targetPath,
            ParentDirectoryId = parentDir.Id,
            BlobId = blob.Id,
            Size = file.Size,
            MimeType = file.MimeType,
            CreatedBy = _currentUserId.ToString(),
            UpdatedBy = _currentUserId.ToString(),
            UserId = _currentUserId
        };

        await _dbContext.Files.AddAsync(newFile);
        await _dbContext.SaveChangesAsync();
    }

    public async Task MoveFileAsync(string path, string newPath)
    {
        var sourcePath = ResolvePath(path);
        var targetPath = ResolvePath(newPath);

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == sourcePath && f.UserId == _currentUserId);
        if (file == null)
            throw new InvalidOperationException($"File '{sourcePath}' not found.");

        var (parentPath, fileName) = SplitPath(targetPath);
        var parentDir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);
        if (parentDir == null)
            throw new InvalidOperationException($"Target directory '{parentPath}' does not exist.");

        file.Name = fileName;
        file.Path = targetPath;
        file.ParentDirectoryId = parentDir.Id;
        file.UpdatedBy = _currentUserId.ToString();
        file.UpdateDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<FsNode?> GetInfoAsync(string path)
    {
        var fullPath = ResolvePath(path);

        // Check Directory
        var dir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == fullPath && d.UserId == _currentUserId);
        if (dir != null) return dir;

        // Otherwise, check File
        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == fullPath && f.UserId == _currentUserId);
        if (file != null) return file;

        return null; // Not found
    }

    public Task SetWorkingDirectoryAsync(string path)
    {
        _workingDirectory = ResolvePath(path);
        return Task.CompletedTask;
    }

    public Task<string> GetWorkingDirectoryAsync()
    {
        return Task.FromResult(_workingDirectory);
    }

    #endregion

    #region Additional Features
    public async Task RenameDirectoryAsync(string oldPath, string newName)
    {
        var sourcePath = ResolvePath(oldPath);

        // 1) Find the directory
        var dir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == sourcePath && d.UserId == _currentUserId);

        if (dir == null)
            throw new InvalidOperationException($"Directory '{oldPath}' not found.");

        // 2) Find parent
        var (parentPath, currentDirName) = SplitPath(sourcePath);

        // 3) Build new path
        var newPath = NormalizePath(parentPath + "/" + newName);

        // 4) Check for conflicts
        var conflict = await _dbContext.Directories
        .AnyAsync(d => d.Path == newPath && d.UserId == _currentUserId);
        if (conflict)
            throw new InvalidOperationException($"A folder named '{newName}' already exists here.");

        // 5) Update this directory
        dir.Name = newName;
        dir.Path = newPath;
        dir.UpdateDate = DateTime.UtcNow;
        dir.UpdatedBy = _currentUserId.ToString();

        // 6) Now recursively fix sub-items’ paths
        await UpdateChildrenPathsRecursive(dir, sourcePath, newPath);

        await _dbContext.SaveChangesAsync();
    }

    public async Task RenameFileAsync(string oldPath, string newName)
    {
        var sourcePath = ResolvePath(oldPath);

        // 1) Find the file
        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Path == sourcePath && f.UserId == _currentUserId);

        if (file == null)
            throw new InvalidOperationException($"File '{oldPath}' not found.");

        // 2) Find parent
        var (parentPath, currentFileName) = SplitPath(sourcePath);

        // 3) Build new path
        var newPath = NormalizePath(parentPath + "/" + newName);

        // 4) Check for conflicts
        var conflict = await _dbContext.Files
            .AnyAsync(f => f.Path == newPath && f.UserId == _currentUserId);
        if (conflict)
            throw new InvalidOperationException($"A file named '{newName}' already exists here.");

        // 5) Update
        file.Name = newName;
        file.Path = newPath;
        file.UpdateDate = DateTime.UtcNow;
        file.UpdatedBy = _currentUserId.ToString();

        await _dbContext.SaveChangesAsync();

    }
    #endregion

    #region Private Helpers
    private string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return _workingDirectory;

        if (!path.StartsWith("/"))
        {
            if (!_workingDirectory.EndsWith("/"))
                _workingDirectory += "/";
            path = _workingDirectory + path;
        }

        return NormalizePath(path);
    }

    private string NormalizePath(string path)
    {
        while (path.Contains("//"))
            path = path.Replace("//", "/");

        if (path.EndsWith("/") && path.Length > 1)
            path = path.TrimEnd('/');

        return path;
    }

    private (string parentPath, string nodeName) SplitPath(string fullPath)
    {
        if (fullPath == "/")
            return (string.Empty, "/");

        var lastSlash = fullPath.LastIndexOf('/');

        if (lastSlash <= 0)
        {
            var name = fullPath.Substring(1);
            return ("/", name);
        }

        if (lastSlash > 0)
        {
            var parent = fullPath.Substring(0, lastSlash);
            var name = fullPath.Substring(lastSlash + 1);
            return (parent, name);
        }

        return (string.Empty, fullPath.Trim('/'));
    }

    private async Task DeleteDirectoryRecursive(DirectoryNode dir)
    {
        // 1. Delete all files in this directory
        var files = await _dbContext.Files
            .Where(f => f.ParentDirectoryId == dir.Id && f.UserId == _currentUserId)
            .ToListAsync();

        foreach (var file in files)
        {
            var blob = await _dbContext.Blobs.FindAsync(file.BlobId);
            if (blob != null)
            {
                blob.ReferenceCount--;
                blob.UpdateDate = DateTime.UtcNow;
            }
            _dbContext.Files.Remove(file);
        }

        // 2. Recursively delete subdirectories
        var subDirs = await _dbContext.Directories
            .Where(d => d.ParentDirectoryId == dir.Id && d.UserId == _currentUserId)
            .ToListAsync();

        foreach (var subDir in subDirs)
        {
            await DeleteDirectoryRecursive(subDir);
        }

        // 3. Finally remove this directory
        _dbContext.Directories.Remove(dir);
    }

    private async Task CopyDirectoryRecursive(DirectoryNode sourceDir, string sourceBase, string targetBase)
    {
        // Create the target directory
        var (parentPath, dirName) = SplitPath(targetBase);

        var parentDir = await _dbContext.Directories
            .FirstOrDefaultAsync(d => d.Path == parentPath && d.UserId == _currentUserId);

        if (!string.IsNullOrEmpty(parentPath) && parentDir == null)
            throw new InvalidOperationException($"Target parent directory '{parentPath}' does not exist.");

        var newDir = new DirectoryNode
        {
            Name = dirName,
            Path = targetBase,
            ParentDirectoryId = parentDir?.Id,
            UserId = _currentUserId,
            MimeType = "inode/directory",
            CreatedBy = _currentUserId.ToString(),
            UpdatedBy = _currentUserId.ToString()
        };

        await _dbContext.Directories.AddAsync(newDir);
        await _dbContext.SaveChangesAsync();

        // Copy files in the directory
        var files = await _dbContext.Files
            .Where(f => f.ParentDirectoryId == sourceDir.Id && f.UserId == _currentUserId)
            .ToListAsync();

        foreach (var file in files)
        {
            var blob = await _dbContext.Blobs.FindAsync(file.BlobId);
            
            if (blob == null)
                throw new InvalidOperationException("Blob not found during copy.");

            blob.ReferenceCount++;
            blob.UpdateDate = DateTime.UtcNow;

            var newFilePath = targetBase + "/" + file.Name;
            
            var newFile = new FileNode
            {
                Name = file.Name,
                Path = newFilePath,
                ParentDirectoryId = newDir.Id,
                BlobId = blob.Id,
                Size = file.Size,
                MimeType = file.MimeType,
                CreatedBy = _currentUserId.ToString(),
                UpdatedBy = _currentUserId.ToString(),
                UserId = _currentUserId
            };

            await _dbContext.Files.AddAsync(newFile);
        }

        // Copy subdirectories recursively
        var subDirs = await _dbContext.Directories
            .Where(sd => sd.ParentDirectoryId == sourceDir.Id && sd.UserId == _currentUserId)
            .ToListAsync();

        foreach (var subDir in subDirs)
        {
            var subRelativePath = subDir.Path.Substring(sourceBase.Length).Trim('/');
            var newSubPath = targetBase + "/" + subRelativePath;
            await CopyDirectoryRecursive(subDir, subDir.Path, newSubPath);
        }
    }

    private async Task UpdateChildrenPathsRecursive(DirectoryNode dir, string oldBase, string newBase)
    {
        var subDirs = await _dbContext.Directories
            .Where(d => d.Path.StartsWith(oldBase) && d.UserId == _currentUserId && d.Id != dir.Id)
            .ToListAsync();

        foreach (var sub in subDirs)
        {
            var relative = sub.Path.Substring(oldBase.Length);
            var newPath = newBase + relative;
            sub.Path = NormalizePath(newPath);
            sub.UpdateDate = DateTime.UtcNow;
            sub.UpdatedBy = _currentUserId.ToString();
        }

        var files = await _dbContext.Files
            .Where(f => f.Path.StartsWith(oldBase) && f.UserId == _currentUserId)
            .ToListAsync();

        foreach (var file in files)
        {
            var relative = file.Path.Substring(oldBase.Length);
            var newPath = newBase + relative;
            file.Path = NormalizePath(newPath);
            file.UpdateDate = DateTime.UtcNow;
            file.UpdatedBy = _currentUserId.ToString();
        }
    }

    private static string ComputeSHA256(byte[] data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}
