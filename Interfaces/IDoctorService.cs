using ClinicFlow.Common;
using ClinicFlow.DTOs.Doctor;

namespace ClinicFlow.Interfaces;

public interface IDoctorService
{
    Task<PagedResponse<DoctorDto>> GetAllAsync(DoctorQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<DoctorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, DoctorDto? Doctor)> CreateAsync(CreateDoctorDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, DoctorDto? Doctor)> UpdateAsync(int id, CreateDoctorDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
