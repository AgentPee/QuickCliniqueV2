namespace QuickClinique.Models;

/// <summary>
/// Entity for storing Data Protection keys in the database.
/// This ensures keys persist across application restarts and deployments.
/// The entity only needs to have Id, FriendlyName, and Xml properties.
/// </summary>
public class DataProtectionKey
{
    public int Id { get; set; }
    public string? FriendlyName { get; set; }
    public string Xml { get; set; } = string.Empty;
}

