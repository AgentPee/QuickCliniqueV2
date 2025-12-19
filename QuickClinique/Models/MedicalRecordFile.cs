using System;

namespace QuickClinique.Models;

public partial class MedicalRecordFile
{
    public int FileId { get; set; }

    public int PatientId { get; set; }

    public int? RecordId { get; set; } // Optional link to specific Precord

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!; // e.g., "image/jpeg", "application/pdf"

    public long FileSize { get; set; } // Size in bytes

    public string? Description { get; set; } // Optional description of the file

    public int? UploadedByStaffId { get; set; } // Staff member who uploaded the file

    public string? UploadedByName { get; set; } // Name of staff member who uploaded

    public DateTime UploadedAt { get; set; } // When the file was uploaded

    public virtual Student Patient { get; set; } = null!;

    public virtual Precord? Record { get; set; }
}


