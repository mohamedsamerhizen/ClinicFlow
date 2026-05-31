using ClinicFlow.Common;
using ClinicFlow.DTOs.Prescription;

namespace ClinicFlow.Interfaces;

public interface IPrescriptionService
{
    Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, PrescriptionDto? Prescription)> CreateAsync(CreatePrescriptionDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, PrescriptionDto? Prescription)> UpdateAsync(int id, UpdatePrescriptionDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
