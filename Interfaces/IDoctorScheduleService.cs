using ClinicFlow.DTOs.DoctorSchedule;

namespace ClinicFlow.Interfaces;

public interface IDoctorScheduleService
{
    Task<List<DoctorScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DoctorScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> CreateAsync(CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> UpdateAsync(int id, CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);
}
