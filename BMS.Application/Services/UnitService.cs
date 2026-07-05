using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _unitRepository;

    public UnitService(IUnitRepository unitRepository)
    {
        _unitRepository = unitRepository;
    }

    public Task<IEnumerable<UnitDto>> GetAllAsync() =>
        _unitRepository.GetAllAsync();

    public async Task<UnitDto> GetByIdAsync(int id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        return unit ?? throw new KeyNotFoundException($"Unit {id} not found.");
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
    {
        if (await _unitRepository.IsUnitNumberTakenAsync(dto.UnitNumber))
            throw new InvalidOperationException($"Unit number '{dto.UnitNumber}' already exists.");

        return await _unitRepository.CreateAsync(dto);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitDto dto)
    {
        if (!await _unitRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Unit {id} not found.");

        if (await _unitRepository.IsUnitNumberTakenAsync(dto.UnitNumber, excludeId: id))
            throw new InvalidOperationException($"Unit number '{dto.UnitNumber}' already exists.");

        await _unitRepository.UpdateAsync(id, dto);
        return (await _unitRepository.GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _unitRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Unit {id} not found.");

        await _unitRepository.DeleteAsync(id);
    }
}
