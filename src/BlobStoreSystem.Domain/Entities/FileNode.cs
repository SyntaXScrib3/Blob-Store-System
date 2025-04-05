using System.ComponentModel.DataAnnotations.Schema;

namespace BlobStoreSystem.Domain.Entities;

public class FileNode : FsNode
{
    public override bool IsDirectory => false;

    public Guid BlobId { get; set; }
    public Guid UserId { get; set; }

    public Guid? ParentDirectoryId { get; set; }

    [ForeignKey(nameof(ParentDirectoryId))]
    public DirectoryNode? ParentDirectory { get; set; }
}
