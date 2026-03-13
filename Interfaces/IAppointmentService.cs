using ClinicFlow.Common;
using ClinicFlow.DTOs.Appointment;

namespace ClinicFlow.Interfaces;

public interface IAppointmentService
{
    Task<PagedResponse<AppointmentDto>> GetAllAsync(AppointmentQueryParams queryParams);
    Task<AppointmentDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, AppointmentDto? Appointment)> CreateAsync(CreateAppointmentDto dto);
    Task<(bool Success, string Message)> ConfirmAsync(int id);
    Task<(bool Success, string Message)> CancelAsync(int id);
    Task<(bool Success, string Message)> CompleteAsync(int id);
    Task<List<UpcomingAppointmentDto>> GetUpcomingAsync(int days = 7);
}