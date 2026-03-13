using ClinicFlow.Data;
using ClinicFlow.DTOs.DoctorSchedule;
using ClinicFlow.Entities;
using ClinicFlow.Enums;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class DoctorScheduleService : IDoctorScheduleService
{
    private readonly AppDbContext _context;

    public DoctorScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorScheduleDto>> GetAllAsync()
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Select(s => new DoctorScheduleDto
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor != null ? s.Doctor.FullName : string.Empty,
                SpecializationName = s.Doctor != null && s.Doctor.Specialization != null ? s.Doctor.Specialization.Name : string.Empty,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync();
    }

    public async Task<DoctorScheduleDto?> GetByIdAsync(int id)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new DoctorScheduleDto
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor != null ? s.Doctor.FullName : string.Empty,
                SpecializationName = s.Doctor != null && s.Doctor.Specialization != null ? s.Doctor.Specialization.Name : string.Empty,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> CreateAsync(CreateDoctorScheduleDto dto)
    {
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId);
        if (!doctorExists) return (false, "Invalid doctor id.", null);
        if (dto.StartTime >= dto.EndTime) return (false, "Start time must be earlier than end time.", null);

        var hasOverlap = await _context.DoctorSchedules.AnyAsync(s =>
            s.DoctorId == dto.DoctorId &&
            s.DayOfWeek == dto.DayOfWeek &&
            dto.StartTime < s.EndTime &&
            dto.EndTime > s.StartTime);

        if (hasOverlap) return (false, "This doctor already has an overlapping schedule on this day.", null);

        var schedule = new DoctorSchedule
        {
            DoctorId = dto.DoctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        _context.DoctorSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var created = await GetByIdAsync(schedule.Id);
        return (true, "Schedule created successfully.", created);
    }

    public async Task<(bool Success, string Message, DoctorScheduleDto? Schedule)> UpdateAsync(int id, CreateDoctorScheduleDto dto)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(id);
        if (schedule is null) return (false, "Schedule not found.", null);

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId);
        if (!doctorExists) return (false, "Invalid doctor id.", null);
        if (dto.StartTime >= dto.EndTime) return (false, "Start time must be earlier than end time.", null);

        var hasOverlap = await _context.DoctorSchedules.AnyAsync(s =>
            s.Id != id &&
            s.DoctorId == dto.DoctorId &&
            s.DayOfWeek == dto.DayOfWeek &&
            dto.StartTime < s.EndTime &&
            dto.EndTime > s.StartTime);

        if (hasOverlap) return (false, "This doctor already has an overlapping schedule on this day.", null);

        var now = DateTime.Now;

        var hasAffectedFutureAppointments = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == schedule.DoctorId &&
            a.AppointmentDate > now &&
            a.Status != AppointmentStatus.Cancelled &&
            a.AppointmentDate.DayOfWeek == schedule.DayOfWeek &&
            a.AppointmentDate.TimeOfDay >= schedule.StartTime &&
            a.AppointmentDate.TimeOfDay < schedule.EndTime &&
            (
                dto.DoctorId != schedule.DoctorId ||
                dto.DayOfWeek != schedule.DayOfWeek ||
                a.AppointmentDate.TimeOfDay < dto.StartTime ||
                a.AppointmentDate.TimeOfDay >= dto.EndTime
            ));

        if (hasAffectedFutureAppointments)
            return (false, "Cannot update schedule because there are future appointments that would become outside the new working hours.", null);

        schedule.DoctorId = dto.DoctorId;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(schedule.Id);
        return (true, "Schedule updated successfully.", updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(id);
        if (schedule is null) return (false, "Schedule not found.");

        var now = DateTime.Now;
        var hasFutureAppointmentsInThisSchedule = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == schedule.DoctorId &&
            a.AppointmentDate > now &&
            a.Status != AppointmentStatus.Cancelled &&
            a.AppointmentDate.DayOfWeek == schedule.DayOfWeek &&
            a.AppointmentDate.TimeOfDay >= schedule.StartTime &&
            a.AppointmentDate.TimeOfDay < schedule.EndTime);

        if (hasFutureAppointmentsInThisSchedule)
            return (false, "Cannot delete schedule because there are future appointments within this schedule.");

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return (true, "Schedule deleted successfully.");
    }

    public async Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(int doctorId, DateTime date)
    {
        var doctor = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.Id == doctorId)
            .Select(d => new
            {
                d.Id,
                d.FullName,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .FirstOrDefaultAsync();

        if (doctor is null)
            return null;

        var dayOfWeek = date.DayOfWeek;
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        var workingSlots = await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek)
            .OrderBy(s => s.StartTime)
            .Select(s => new DoctorDailyWorkingSlotDto
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new DoctorDailyScheduleItemDto
            {
                AppointmentId = a.Id,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status.ToString(),
                PatientId = a.PatientId,
                PatientName = patientsQuery
                    .Where(p => p.Id == a.PatientId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();

        return new DoctorDailyScheduleDto
        {
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            SpecializationName = doctor.SpecializationName,
            Date = date.Date,
            IsWorkingDay = workingSlots.Any(),
            WorkingSlots = workingSlots,
            Appointments = appointments
        };
    }
}