using ClinicFlow.Common;
using ClinicFlow.DTOs.Doctor;

namespace ClinicFlow.Interfaces;

public interface IDoctorService
{
    Task<PagedResponse<DoctorDto>> GetAllAsync(DoctorQueryParams queryParams);
    Task<DoctorDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, DoctorDto? Doctor)> CreateAsync(CreateDoctorDto dto);
    Task<(bool Success, string Message, DoctorDto? Doctor)> UpdateAsync(int id, CreateDoctorDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}