using BMS.API.Wrappers;
using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService) => _unitService = unitService;

    /// <summary>GET /api/units — list all units</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UnitDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var units = await _unitService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<UnitDto>>.Ok(units));
    }

    /// <summary>GET /api/units/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var unit = await _unitService.GetByIdAsync(id);
        return Ok(ApiResponse<UnitDto>.Ok(unit));
    }

    /// <summary>POST /api/units — create a unit</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
    {
        var unit = await _unitService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = unit.Id },
            ApiResponse<UnitDto>.Ok(unit, "Unit created successfully."));
    }

    /// <summary>PUT /api/units/{id} — update a unit</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitDto dto)
    {
        var unit = await _unitService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UnitDto>.Ok(unit, "Unit updated successfully."));
    }

    /// <summary>DELETE /api/units/{id} — delete a unit (blocked if leases exist)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _unitService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Unit deleted successfully."));
    }
}
