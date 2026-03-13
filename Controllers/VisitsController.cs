using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Visit;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class VisitsController : ControllerBase
{
    private readonly IVisitService _visitService;

    public VisitsController(IVisitService visitService)
    {
        _visitService = visitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] VisitQueryParams queryParams)
    {
        var visits = await _visitService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(visits));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var visit = await _visitService.GetByIdAsync(id);
        if (visit is null) return NotFound(ApiResponse<string>.FailResponse("Visit not found."));
        return Ok(ApiResponse<object>.SuccessResponse(visit));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Create(CreateVisitDto dto)
    {
        var result = await _visitService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Visit!.Id },
            ApiResponse<object>.SuccessResponse(result.Visit, "Visit created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Update(int id, UpdateVisitDto dto)
    {
        var result = await _visitService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Visit not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Visit, "Visit updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _visitService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Visit not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}