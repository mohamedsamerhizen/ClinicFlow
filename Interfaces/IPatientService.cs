using ClinicFlow.Common;
using ClinicFlow.DTOs.Patient;

namespace ClinicFlow.Interfaces;

public interface IPatientService
{
    Task<PagedResponse<PatientDto>> GetAllAsync(PatientQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<PatientDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PatientHistoryDto?> GetHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task<PatientSummaryDto?> GetSummaryAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, PatientDto? Patient)> CreateAsync(CreatePatientDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, PatientDto? Patient)> UpdateAsync(int id, CreatePatientDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
