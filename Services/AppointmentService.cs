using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Appointment;
using ClinicFlow.Entities;
using ClinicFlow.Enums;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppointmentService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<AppointmentDto>> GetAllAsync(AppointmentQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .AsQueryable();

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
            {
                return new PagedResponse<AppointmentDto>
                {
                    Items = new List<AppointmentDto>(),
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    TotalCount = 0
                };
            }

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }
        else
        {
            if (queryParams.DoctorId.HasValue)
                query = query.Where(a => a.DoctorId == queryParams.DoctorId.Value);
        }

        if (queryParams.PatientId.HasValue)
            query = query.Where(a => a.PatientId == queryParams.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Status) &&
            Enum.TryParse<AppointmentStatus>(queryParams.Status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        if (queryParams.Date.HasValue)
        {
            var date = queryParams.Date.Value.Date;
            var nextDate = date.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= date && a.AppointmentDate < nextDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectAppointmentQuery(query)
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<AppointmentDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return null;

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }

        return await ProjectAppointmentQuery(query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message, AppointmentDto? Appointment)> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.AppointmentDate <= DateTime.Now)
            return (false, "Appointment date must be in the future.", null);

        if (dto.AppointmentDate.Second != 0 || dto.AppointmentDate.Millisecond != 0)
            return (false, "Appointment time must not include seconds or milliseconds.", null);

        if (dto.AppointmentDate.Ticks % TimeSpan.FromMinutes(15).Ticks != 0)
            return (false, "Appointment time must be on a 15-minute interval. Example: 09:00, 09:15, 09:30, 09:45.", null);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return (false, "No doctor profile is linked to the current user.", null);

            if (dto.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to create appointments for another doctor.", null);
        }

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId, cancellationToken);
        if (!doctorExists)
            return (false, "Invalid doctor id.", null);

        var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId, cancellationToken);
        if (!patientExists)
            return (false, "Invalid patient id.", null);

        var appointmentDay = dto.AppointmentDate.DayOfWeek;
        var appointmentTime = dto.AppointmentDate.TimeOfDay;

        var doctorSchedules = await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == dto.DoctorId && s.DayOfWeek == appointmentDay)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);

        var isWithinDoctorSchedule = doctorSchedules.Any(s =>
            appointmentTime >= s.StartTime && appointmentTime < s.EndTime);

        if (!isWithinDoctorSchedule)
            return (false, "Appointment time is outside the doctor's working schedule.", null);

        var hasDoctorConflict = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == dto.DoctorId &&
            a.AppointmentDate == dto.AppointmentDate &&
            a.Status != AppointmentStatus.Cancelled,
            cancellationToken);

        if (hasDoctorConflict)
            return (false, "This doctor already has an appointment at this time.", null);

        var hasPatientConflict = await _context.Appointments.AnyAsync(a =>
            a.PatientId == dto.PatientId &&
            a.AppointmentDate == dto.AppointmentDate &&
            a.Status != AppointmentStatus.Cancelled,
            cancellationToken);

        if (hasPatientConflict)
            return (false, "This patient already has an appointment at this time.", null);

        var appointment = new Appointment
        {
            DoctorId = dto.DoctorId,
            PatientId = dto.PatientId,
            AppointmentDate = dto.AppointmentDate,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (false, "Appointment conflict detected. The selected time slot is no longer available.", null);
        }

        var created = await GetByIdAsync(appointment.Id, cancellationToken);
        return (true, "Appointment created successfully.", created);
    }

    public async Task<(bool Success, string Message)> ConfirmAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return (false, "Appointment not found.");

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue || appointment.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to confirm this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
            return (false, "Cancelled appointment cannot be confirmed.");

        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Completed appointment cannot be confirmed.");

        if (appointment.Status == AppointmentStatus.Confirmed)
            return (false, "Appointment is already confirmed.");

        appointment.Status = AppointmentStatus.Confirmed;
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "Appointment confirmed.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return (false, "Appointment not found.");

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue || appointment.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to cancel this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Completed appointment cannot be cancelled.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return (false, "Appointment is already cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "Appointment cancelled.");
    }

    public async Task<(bool Success, string Message)> CompleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return (false, "Appointment not found.");

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue || appointment.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to complete this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
            return (false, "Cancelled appointment cannot be completed.");

        if (appointment.Status == AppointmentStatus.Pending)
            return (false, "Pending appointment must be confirmed before completion.");

        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Appointment is already completed.");

        appointment.Status = AppointmentStatus.Completed;
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "Appointment completed.");
    }

    public async Task<List<UpcomingAppointmentDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
            days = 7;

        var now = DateTime.Now;
        var endDate = now.AddDays(days);

        var query = _context.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= now &&
                        a.AppointmentDate <= endDate &&
                        a.Status != AppointmentStatus.Cancelled);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return new List<UpcomingAppointmentDto>();

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var specializationsQuery = _context.Specializations.IgnoreQueryFilters();

        return await query
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new UpcomingAppointmentDto
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status.ToString(),
                DoctorId = a.DoctorId,
                DoctorName = doctorsQuery.Where(d => d.Id == a.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty,
                SpecializationName = (
                    from d in doctorsQuery
                    join s in specializationsQuery on d.SpecializationId equals s.Id
                    where d.Id == a.DoctorId
                    select s.Name
                ).FirstOrDefault() ?? string.Empty,
                PatientId = a.PatientId,
                PatientName = patientsQuery.Where(p => p.Id == a.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AppointmentDto> ProjectAppointmentQuery(IQueryable<Appointment> query)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var specializationsQuery = _context.Specializations.IgnoreQueryFilters();

        return query.Select(a => new AppointmentDto
        {
            Id = a.Id,
            AppointmentDate = a.AppointmentDate,
            Status = a.Status.ToString(),
            DoctorId = a.DoctorId,
            DoctorName = doctorsQuery.Where(d => d.Id == a.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty,
            PatientId = a.PatientId,
            PatientName = patientsQuery.Where(p => p.Id == a.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty,
            SpecializationName = (
                from d in doctorsQuery
                join s in specializationsQuery on d.SpecializationId equals s.Id
                where d.Id == a.DoctorId
                select s.Name
            ).FirstOrDefault() ?? string.Empty
        });
    }

    private bool IsDoctor()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(AppRoles.Doctor) == true;
    }

    private async Task<int?> GetCurrentDoctorIdAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.ApplicationUserId == userId)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
