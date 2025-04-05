namespace BlobStoreSystem.Domain.Entities;

public abstract class FsNode
{
    /// <summary>
    /// Unique identifier for the node (could be a GUID in a real DB).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The name of the file or directory (e.g., "MyFile.txt" or "Documents").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The path to this node (e.g., "/Documents/MyFile.txt").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes (for a directory, might be 0 or the sum of subfiles).
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// MIME type for files (e.g. "text/plain"); for directories, could be null or "inode/directory".
    /// </summary>
    public string? MimeType { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// A helper property to distinguish between a FileNode or DirectoryNode, if needed.
    /// </summary>
    public abstract bool IsDirectory { get; }
}