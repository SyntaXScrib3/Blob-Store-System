namespace BlobStoreSystem.Domain.Entities;

public class Blob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Content hash (e.g., SHA-256) to identify duplicate data.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Reference count to track how many FileNodes refer to this Blob.
    /// </summary>
    public int ReferenceCount { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
}
