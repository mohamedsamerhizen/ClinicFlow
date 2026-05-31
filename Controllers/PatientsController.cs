using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Patient;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PatientQueryParams queryParams)
    {
        var patients = await _patientService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(patients));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(patient));
    }

    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _patientService.GetSummaryAsync(id);
        if (summary is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(ApiResponse<string>.FailResponse("Patient name is required."));

        var patients = await _patientService.SearchByNameAsync(name);
        return Ok(ApiResponse<object>.SuccessResponse(patients));
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await _patientService.GetHistoryAsync(id);
        if (history is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(history));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Create(CreatePatientDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        if (!result.Success)
            return BadRequest(ApiResponse<string>.FailResponse(result.Message));

        return Created(
            $"/api/patients/{result.Patient!.Id}",
            ApiResponse<object>.SuccessResponse(result.Patient, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Update(int id, CreatePatientDto dto)
    {
        var result = await _patientService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Patient not found.")
                return NotFound(ApiResponse<string>.FailResponse(result.Message));

            return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Patient, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patientService.DeleteAsync(id);
        if (!result.Success) return BadRequest(ApiResponse<string>.FailResponse(result.Message));
        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
