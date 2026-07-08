using BMS.Application.DTOs.Units;

namespace BMS.Application.Interfaces.Repositories;

public interface IUnitRepository
{
    Task<UnitDto?>             GetByIdAsync(int id);
    Task<IEnumerable<UnitDto>> GetAllAsync();
    Task<UnitDto>              CreateAsync(CreateUnitDto dto);
    Task                       UpdateAsync(int id, UpdateUnitDto dto);
    Task                       DeleteAsync(int id);
    Task<bool>                 ExistsAsync(int id);
    Task<bool>                 IsUnitNumberTakenAsync(string unitNumber, int? excludeId = null);

    /// <summary>
    /// Returns the number of units already on a given floor.
    /// Used to enforce the max-3-units-per-floor business rule.
    /// </summary>
    Task<int> CountByFloorAsync(int floorNumber, int? excludeUnitId = null);

    /// <summary>
    /// Returns units that can be selected when creating a lease for <paramref name="tenantId"/>:
    /// — All units with Status == Available (any tenant can use these).
    /// — Units with Status == Reserved where ReservedForTenantId == tenantId
    ///   (the unit was set aside specifically for this tenant).
    /// Each result has <see cref="UnitDto.IsReservedForRequestedTenant"/> set to true
    /// for the reserved-for-this-tenant units so the UI can pin them at the top.
    /// </summary>
    Task<IEnumerable<UnitDto>> GetSelectableForLeaseAsync(int tenantId);

    /// <summary>
    /// Marks the unit as Reserved for the given tenant and persists the change.
    /// Only Available or already-Reserved units may be reserved.
    /// </summary>
    Task<UnitDto> ReserveAsync(int unitId, int tenantId);
}
