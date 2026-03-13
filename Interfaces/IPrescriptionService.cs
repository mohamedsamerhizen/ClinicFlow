using ClinicFlow.Common;
using ClinicFlow.DTOs.Prescription;

namespace ClinicFlow.Interfaces;

public interface IPrescriptionService
{
    Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams);
    Task<PrescriptionDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, PrescriptionDto? Prescription)> CreateAsync(CreatePrescriptionDto dto);
    Task<(bool Success, string Message, PrescriptionDto? Prescription)> UpdateAsync(int id, UpdatePrescriptionDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}