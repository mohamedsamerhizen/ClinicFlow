using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.DoctorSchedule;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class DoctorSchedulesController : ControllerBase
{
    private readonly IDoctorScheduleService _doctorScheduleService;

    public DoctorSchedulesController(IDoctorScheduleService doctorScheduleService)
    {
        _doctorScheduleService = doctorScheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schedules = await _doctorScheduleService.GetAllAsync();
        return Ok(ApiResponse<object>.SuccessResponse(schedules));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _doctorScheduleService.GetByIdAsync(id);
        if (schedule is null) return NotFound(ApiResponse<string>.FailResponse("Schedule not found."));
        return Ok(ApiResponse<object>.SuccessResponse(schedule));
    }

    [HttpGet("doctor/{doctorId}/daily")]
    public async Task<IActionResult> GetDoctorDailySchedule(int doctorId, [FromQuery] DateTime date)
    {
        var result = await _doctorScheduleService.GetDoctorDailyScheduleAsync(doctorId, date);
        if (result is null) return NotFound(ApiResponse<string>.FailResponse("Doctor not found."));
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateDoctorScheduleDto dto)
    {
        var result = await _doctorScheduleService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Schedule!.Id },
            ApiResponse<object>.SuccessResponse(result.Schedule, "Schedule created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, CreateDoctorScheduleDto dto)
    {
        var result = await _doctorScheduleService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Schedule not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Schedule, "Schedule updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _doctorScheduleService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Schedule not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}