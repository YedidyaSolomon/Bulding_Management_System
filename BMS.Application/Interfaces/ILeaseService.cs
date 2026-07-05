using BMS.Application.DTOs.Leases;

namespace BMS.Application.Interfaces;

public interface ILeaseService
{
    Task<IEnumerable<LeaseDto>> GetAllAsync();
    Task<LeaseDto>              GetByIdAsync(int id);
    Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId);
    Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId);
    Task<LeaseDto>              CreateAsync(CreateLeaseDto dto);
    Task<LeaseDto>              UpdateAsync(int id, UpdateLeaseDto dto);
    Task                        TerminateAsync(int id, TerminateLeaseDto dto);
}
