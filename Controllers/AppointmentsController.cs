using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Appointment;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AppointmentQueryParams queryParams)
    {
        var appointments = await _appointmentService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(appointments));
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7)
    {
        var appointments = await _appointmentService.GetUpcomingAsync(days);
        return Ok(ApiResponse<object>.SuccessResponse(appointments));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment is null) return NotFound(ApiResponse<string>.FailResponse("Appointment not found."));
        return Ok(ApiResponse<object>.SuccessResponse(appointment));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Create(CreateAppointmentDto dto)
    {
        var result = await _appointmentService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Appointment!.Id },
            ApiResponse<object>.SuccessResponse(result.Appointment, "Appointment created successfully."));
    }

    [HttpPatch("{id}/confirm")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _appointmentService.ConfirmAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Appointment not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _appointmentService.CancelAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Appointment not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/complete")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _appointmentService.CompleteAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Appointment not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}