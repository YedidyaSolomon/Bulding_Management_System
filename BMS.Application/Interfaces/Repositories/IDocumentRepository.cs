using BMS.Application.DTOs.Documents;

namespace BMS.Application.Interfaces.Repositories;

public interface IDocumentRepository
{
    /// <summary>
    /// Returns all documents with an ExpiryDate that falls within the next
    /// <paramref name="withinDays"/> days (inclusive of today).
    /// </summary>
    Task<IEnumerable<ExpiringDocumentDto>> GetExpiringAsync(int withinDays);
}
