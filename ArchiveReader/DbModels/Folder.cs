using System;
using System.Collections.Generic;

namespace ArchiveReader.DbModels;

public partial class Folder
{
    public Guid Id { get; set; }

    public string DatabaseName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string FolderType { get; set; } = null!;

    public Guid? FolderId { get; set; }

    public virtual Folder? FolderNavigation { get; set; }

    public virtual ICollection<GroupFolder> GroupFolders { get; } = new List<GroupFolder>();

    public virtual ICollection<Folder> InverseFolderNavigation { get; } = new List<Folder>();
}
