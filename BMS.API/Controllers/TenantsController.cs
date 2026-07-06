using BMS.API.Wrappers;
using BMS.Application.DTOs.Tenants;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService) => _tenantService = tenantService;

    /// <summary>GET /api/tenants — Admin/Manager: all tenants</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<TenantDto>>.Ok(tenants));
    }

    /// <summary>GET /api/tenants/mine — Viewer: only tenants linked to the calling user</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantDto>>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("User ID not found in token.");

        var tenants = await _tenantService.GetByUserIdAsync(userId);
        return Ok(ApiResponse<IEnumerable<TenantDto>>.Ok(tenants));
    }

    /// <summary>GET /api/tenants/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        return Ok(ApiResponse<TenantDto>.Ok(tenant));
    }

    /// <summary>
    /// POST /api/tenants — register a new tenant.
    /// The request body must include <c>UserEmail</c> of an already-registered account.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto)
    {
        var tenant = await _tenantService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id },
            ApiResponse<TenantDto>.Ok(tenant, "Tenant registered successfully."));
    }

    /// <summary>PUT /api/tenants/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantDto dto)
    {
        var tenant = await _tenantService.UpdateAsync(id, dto);
        return Ok(ApiResponse<TenantDto>.Ok(tenant, "Tenant updated successfully."));
    }

    /// <summary>DELETE /api/tenants/{id} — soft-deactivate</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _tenantService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Tenant deactivated successfully."));
    }

    /// <summary>POST /api/tenants/{id}/documents — upload a legal document</summary>
    [HttpPost("{id:int}/documents")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LegalDocumentDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AddDocument(int id, [FromBody] CreateLegalDocumentDto dto)
    {
        dto.TenantId = id;
        var doc = await _tenantService.AddDocumentAsync(dto);
        return StatusCode(201, ApiResponse<LegalDocumentDto>.Ok(doc, "Document uploaded successfully."));
    }

    /// <summary>GET /api/tenants/{id}/documents — list documents for a tenant</summary>
    [HttpGet("{id:int}/documents")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LegalDocumentDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetDocuments(int id)
    {
        var docs = await _tenantService.GetDocumentsAsync(id);
        return Ok(ApiResponse<IEnumerable<LegalDocumentDto>>.Ok(docs));
    }

    /// <summary>
    /// GET /api/tenants/registered-users — Admin/Manager only.
    /// Returns the email + full name of every active registered user account,
    /// used by the tenant-creation form to validate and pick a user.
    /// </summary>
    [HttpGet("registered-users")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RegisteredUserDto>>), 200)]
    public async Task<IActionResult> GetRegisteredUsers()
    {
        var users = await _tenantService.GetRegisteredUsersAsync();
        var result = users.Select(u => new RegisteredUserDto(u.Email, u.FullName));
        return Ok(ApiResponse<IEnumerable<RegisteredUserDto>>.Ok(result));
    }
}

/// <summary>Lightweight DTO returned to the frontend for the email-picker.</summary>
public record RegisteredUserDto(string Email, string FullName);
