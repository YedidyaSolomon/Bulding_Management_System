using BMS.Application.DTOs.Leases;

namespace BMS.Application.Interfaces.Repositories;

public interface ILeaseRepository
{
    Task<LeaseDto?>             GetByIdAsync(int id);
    Task<IEnumerable<LeaseDto>> GetAllAsync();
    Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId);
    Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId);
    Task<LeaseDto?>             GetActiveLeaseForUnitAsync(int unitId);
    Task<LeaseDto>              CreateAsync(CreateLeaseDto dto);
    Task                        UpdateAsync(int id, UpdateLeaseDto dto);
    Task                        TerminateAsync(int id, string reason);
    Task<bool>                  ExistsAsync(int id);
}
