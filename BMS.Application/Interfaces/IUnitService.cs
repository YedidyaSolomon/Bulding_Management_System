using BMS.Application.DTOs.Units;

namespace BMS.Application.Interfaces;

public interface IUnitService
{
    Task<IEnumerable<UnitDto>> GetAllAsync();
    Task<UnitDto>              GetByIdAsync(int id);
    Task<UnitDto>              CreateAsync(CreateUnitDto dto);
    Task<UnitDto>              UpdateAsync(int id, UpdateUnitDto dto);
    Task                       DeleteAsync(int id);

    /// <summary>
    /// Returns units selectable when creating a lease for the given tenant:
    /// Available units (any tenant) + Reserved units where ReservedForTenantId == tenantId.
    /// </summary>
    Task<IEnumerable<UnitDto>> GetSelectableForLeaseAsync(int tenantId);

    /// <summary>
    /// Marks the unit as Reserved for the given tenant.
    /// Only Available or already-Reserved units may be reserved.
    /// Occupied and UnderMaintenance units are rejected.
    /// </summary>
    Task<UnitDto> ReserveAsync(int unitId, int tenantId);
}
