using System;
using System.Collections.Generic;

namespace ArchiveReader.DbModels;

public partial class AdminDetail
{
    public Guid Id { get; set; }

    public string? AdminGroupIds { get; set; }

    public string? UserGroup { get; set; }

    public string? UserGroupOptions { get; set; }
}
