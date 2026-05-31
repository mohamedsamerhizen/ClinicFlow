using ClinicFlow.Common;
using ClinicFlow.DTOs.Appointment;

namespace ClinicFlow.Interfaces;

public interface IAppointmentService
{
    Task<PagedResponse<AppointmentDto>> GetAllAsync(AppointmentQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, AppointmentDto? Appointment)> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ConfirmAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CompleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<UpcomingAppointmentDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default);
}
