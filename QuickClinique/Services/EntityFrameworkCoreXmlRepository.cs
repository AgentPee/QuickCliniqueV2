using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using System.Xml.Linq;

namespace QuickClinique.Services;

/// <summary>
/// Custom XML repository for Data Protection keys using Entity Framework Core.
/// This allows keys to persist in the database across application restarts.
/// </summary>
public class EntityFrameworkCoreXmlRepository : IXmlRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntityFrameworkCoreXmlRepository> _logger;

    public EntityFrameworkCoreXmlRepository(
        IServiceProvider serviceProvider,
        ILogger<EntityFrameworkCoreXmlRepository> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            var keys = context.DataProtectionKeys.ToList();
            var elements = new List<XElement>();

            foreach (var key in keys)
            {
                try
                {
                    if (!string.IsNullOrEmpty(key.Xml))
                    {
                        var element = XElement.Parse(key.Xml);
                        elements.Add(element);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse XML for key {KeyId}", key.Id);
                }
            }

            return elements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Data Protection keys from database");
            return Array.Empty<XElement>();
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            var xmlString = element.ToString(SaveOptions.DisableFormatting);
            
            var existingKey = context.DataProtectionKeys
                .FirstOrDefault(k => k.FriendlyName == friendlyName);

            if (existingKey != null)
            {
                existingKey.Xml = xmlString;
                context.DataProtectionKeys.Update(existingKey);
            }
            else
            {
                var newKey = new DataProtectionKey
                {
                    FriendlyName = friendlyName,
                    Xml = xmlString
                };
                context.DataProtectionKeys.Add(newKey);
            }

            context.SaveChanges();
            _logger.LogDebug("Stored Data Protection key: {FriendlyName}", friendlyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store Data Protection key: {FriendlyName}", friendlyName);
            throw;
        }
    }
}

