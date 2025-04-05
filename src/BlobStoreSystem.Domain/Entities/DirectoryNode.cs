using System.ComponentModel.DataAnnotations.Schema;

namespace BlobStoreSystem.Domain.Entities;

public class DirectoryNode : FsNode
{
    public override bool IsDirectory => true;
    public Guid UserId { get; set; }

    public Guid? ParentDirectoryId { get; set; }

    [ForeignKey(nameof(ParentDirectoryId))]
    public DirectoryNode? ParentDirectory { get; set; }
}