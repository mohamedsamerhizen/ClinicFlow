using ClinicFlow.Common;
using ClinicFlow.DTOs.Patient;

namespace ClinicFlow.Interfaces;

public interface IPatientService
{
    Task<PagedResponse<PatientDto>> GetAllAsync(PatientQueryParams queryParams);
    Task<PatientDto?> GetByIdAsync(int id);
    Task<List<PatientDto>> SearchByNameAsync(string name);
    Task<PatientHistoryDto?> GetHistoryAsync(int id);
    Task<PatientSummaryDto?> GetSummaryAsync(int id);
    Task<(bool Success, string Message, PatientDto? Patient)> CreateAsync(CreatePatientDto dto);
    Task<(bool Success, string Message, PatientDto? Patient)> UpdateAsync(int id, CreatePatientDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}