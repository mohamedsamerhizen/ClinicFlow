using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Specialization;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationsController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.None, NoStore = false)]
    public async Task<IActionResult> GetAll()
    {
        var specializations = await _specializationService.GetAllAsync();
        return Ok(ApiResponse<object>.SuccessResponse(specializations));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var specialization = await _specializationService.GetByIdAsync(id);
        if (specialization is null) return NotFound(ApiResponse<string>.FailResponse("Specialization not found."));
        return Ok(ApiResponse<object>.SuccessResponse(specialization));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateSpecializationDto dto)
    {
        var result = await _specializationService.CreateAsync(dto);
        if (!result.Success) return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return Created(
            $"/api/specializations/{result.Specialization!.Id}",
            ApiResponse<object>.SuccessResponse(result.Specialization, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, CreateSpecializationDto dto)
    {
        var result = await _specializationService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            if (result.Message == "Specialization not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Specialization, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _specializationService.DeleteAsync(id);
        if (!result.Success)
        {
            if (result.Message == "Specialization not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
