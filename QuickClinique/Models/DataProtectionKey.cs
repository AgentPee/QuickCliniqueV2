using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace QuickClinique.Models;

/// <summary>
/// Entity for storing Data Protection keys in the database.
/// This ensures keys persist across application restarts and deployments.
/// </summary>
public class DataProtectionKey : IDataProtectionKey
{
    public int Id { get; set; }
    public string FriendlyName { get; set; } = string.Empty;
    public string Xml { get; set; } = string.Empty;
}

