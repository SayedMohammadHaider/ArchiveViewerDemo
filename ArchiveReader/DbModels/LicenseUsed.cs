using System;
using System.Collections.Generic;

namespace ArchiveReader.DbModels;

public partial class LicenseUsed
{
    public string? ReplicaId { get; set; }

    public string? LicenseKey { get; set; }

    public Guid Unid { get; set; }
}
