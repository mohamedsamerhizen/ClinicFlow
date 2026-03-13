using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Doctor;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DoctorQueryParams queryParams)
    {
        var doctors = await _doctorService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(doctors));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        if (doctor is null) return NotFound(ApiResponse<string>.FailResponse("Doctor not found."));
        return Ok(ApiResponse<object>.SuccessResponse(doctor));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateDoctorDto dto)
    {
        var result = await _doctorService.CreateAsync(dto);
        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Doctor!.Id },
            ApiResponse<object>.SuccessResponse(result.Doctor, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, CreateDoctorDto dto)
    {
        var result = await _doctorService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Doctor not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Doctor, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _doctorService.DeleteAsync(id);
        if (!result.Success) return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}