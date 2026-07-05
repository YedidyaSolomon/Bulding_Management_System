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
}
