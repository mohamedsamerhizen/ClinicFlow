using ClinicFlow.DTOs.DoctorSchedule;

namespace ClinicFlow.Interfaces;

public interface IDoctorScheduleService
{
    Task<List<DoctorScheduleDto>> GetAllAsync();
    Task<DoctorScheduleDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> CreateAsync(CreateDoctorScheduleDto dto);
    Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> UpdateAsync(int id, CreateDoctorScheduleDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(int doctorId, DateTime date);
}