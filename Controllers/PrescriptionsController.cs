using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Prescription;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionsController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PrescriptionQueryParams queryParams)
    {
        var prescriptions = await _prescriptionService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(prescriptions));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var prescription = await _prescriptionService.GetByIdAsync(id);
        if (prescription is null) return NotFound(ApiResponse<string>.FailResponse("Prescription not found."));
        return Ok(ApiResponse<object>.SuccessResponse(prescription));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Create(CreatePrescriptionDto dto)
    {
        var result = await _prescriptionService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return Created(
            $"/api/prescriptions/{result.Prescription!.Id}",
            ApiResponse<object>.SuccessResponse(result.Prescription, "Prescription created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Update(int id, UpdatePrescriptionDto dto)
    {
        var result = await _prescriptionService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Prescription not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Prescription, "Prescription updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _prescriptionService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Prescription not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
