using BMS.Application.DTOs.Units;

namespace BMS.Application.Interfaces;

public interface IUnitService
{
    Task<IEnumerable<UnitDto>> GetAllAsync();
    Task<UnitDto>              GetByIdAsync(int id);
    Task<UnitDto>              CreateAsync(CreateUnitDto dto);
    Task<UnitDto>              UpdateAsync(int id, UpdateUnitDto dto);
    Task                       DeleteAsync(int id);
}
