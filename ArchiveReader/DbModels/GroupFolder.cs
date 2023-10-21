using System;
using System.Collections.Generic;

namespace ArchiveReader.DbModels;

public partial class GroupFolder
{
    public Guid Id { get; set; }

    public Guid FolderId { get; set; }

    public string GroupId { get; set; } = null!;

    public string? GroupName { get; set; }

    public virtual Folder Folder { get; set; } = null!;
}
