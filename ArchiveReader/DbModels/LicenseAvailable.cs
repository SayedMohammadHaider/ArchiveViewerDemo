using System;
using System.Collections.Generic;

namespace ArchiveReader.DbModels;

public partial class LicenseAvailable
{
    public string? Licensekey { get; set; }

    public string? Consumption { get; set; }

    public string? Capacity { get; set; }

    public string? Customer { get; set; }

    public string? Type { get; set; }

    public string? ExpiryDate { get; set; }
}
