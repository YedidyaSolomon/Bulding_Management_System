using BMS.API.Wrappers;
using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/leases")]
[Authorize]
public class LeasesController : ControllerBase
{
    private readonly ILeaseService _leaseService;

    public LeasesController(ILeaseService leaseService) => _leaseService = leaseService;

    /// <summary>GET /api/leases</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaseDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var leases = await _leaseService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<LeaseDto>>.Ok(leases));
    }

    /// <summary>GET /api/leases/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var lease = await _leaseService.GetByIdAsync(id);
        return Ok(ApiResponse<LeaseDto>.Ok(lease));
    }

    /// <summary>GET /api/leases/by-tenant/{tenantId}</summary>
    [HttpGet("by-tenant/{tenantId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaseDto>>), 200)]
    public async Task<IActionResult> GetByTenant(int tenantId)
    {
        var leases = await _leaseService.GetByTenantIdAsync(tenantId);
        return Ok(ApiResponse<IEnumerable<LeaseDto>>.Ok(leases));
    }

    /// <summary>GET /api/leases/by-unit/{unitId}</summary>
    [HttpGet("by-unit/{unitId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaseDto>>), 200)]
    public async Task<IActionResult> GetByUnit(int unitId)
    {
        var leases = await _leaseService.GetByUnitIdAsync(unitId);
        return Ok(ApiResponse<IEnumerable<LeaseDto>>.Ok(leases));
    }

    /// <summary>POST /api/leases — create a lease</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Create([FromBody] CreateLeaseDto dto)
    {
        var lease = await _leaseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = lease.Id },
            ApiResponse<LeaseDto>.Ok(lease, "Lease created successfully."));
    }

    /// <summary>PUT /api/leases/{id}/renew — renew (extend) a lease</summary>
    [HttpPut("{id:int}/renew")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Renew(int id, [FromBody] UpdateLeaseDto dto)
    {
        var lease = await _leaseService.UpdateAsync(id, dto);
        return Ok(ApiResponse<LeaseDto>.Ok(lease, "Lease renewed successfully."));
    }

    /// <summary>PUT /api/leases/{id}/terminate — terminate a lease early</summary>
    [HttpPut("{id:int}/terminate")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Terminate(int id, [FromBody] TerminateLeaseDto dto)
    {
        await _leaseService.TerminateAsync(id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Lease terminated successfully."));
    }
}
