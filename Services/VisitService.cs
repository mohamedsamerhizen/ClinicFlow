using ClinicFlow.Common;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Visit;
using ClinicFlow.Entities;
using ClinicFlow.Enums;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class VisitService : IVisitService
{
    private readonly AppDbContext _context;

    public VisitService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<VisitDto>> GetAllAsync(VisitQueryParams queryParams)
    {
        var query = _context.Visits.AsNoTracking().AsQueryable();

        if (queryParams.DoctorId.HasValue)
            query = query.Where(v => v.Appointment != null && v.Appointment.DoctorId == queryParams.DoctorId.Value);

        if (queryParams.PatientId.HasValue)
            query = query.Where(v => v.Appointment != null && v.Appointment.PatientId == queryParams.PatientId.Value);

        if (queryParams.Date.HasValue)
        {
            var date = queryParams.Date.Value.Date;
            query = query.Where(v => v.Appointment != null && v.Appointment.AppointmentDate.Date == date);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim();
            query = query.Where(v =>
                v.Symptoms.Contains(search) ||
                v.Diagnosis.Contains(search) ||
                v.Notes.Contains(search) ||
                (v.Appointment != null && v.Appointment.Patient != null && v.Appointment.Patient.FullName.Contains(search)) ||
                (v.Appointment != null && v.Appointment.Doctor != null && v.Appointment.Doctor.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        var items = await query
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(v => new VisitDto
            {
                Id = v.Id,
                AppointmentId = v.AppointmentId,
                AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
                PatientName = v.Appointment != null
                    ? patientsQuery
                        .Where(p => p.Id == v.Appointment.PatientId)
                        .Select(p => p.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = v.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == v.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Symptoms = v.Symptoms,
                Diagnosis = v.Diagnosis,
                Notes = v.Notes
            })
            .ToListAsync();

        return new PagedResponse<VisitDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<VisitDto?> GetByIdAsync(int id)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        return await _context.Visits
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VisitDto
            {
                Id = v.Id,
                AppointmentId = v.AppointmentId,
                AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
                PatientName = v.Appointment != null
                    ? patientsQuery
                        .Where(p => p.Id == v.Appointment.PatientId)
                        .Select(p => p.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = v.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == v.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Symptoms = v.Symptoms,
                Diagnosis = v.Diagnosis,
                Notes = v.Notes
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, VisitDto? Visit)> CreateAsync(CreateVisitDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Symptoms))
            return (false, "Symptoms are required.", null);

        if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            return (false, "Diagnosis is required.", null);

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);
        if (appointment is null) return (false, "Invalid appointment id.", null);
        if (appointment.Status == AppointmentStatus.Cancelled) return (false, "Cannot create visit for a cancelled appointment.", null);
        if (appointment.Status == AppointmentStatus.Pending) return (false, "Appointment should be confirmed before creating a visit.", null);

        var visitExists = await _context.Visits.AnyAsync(v => v.AppointmentId == dto.AppointmentId);
        if (visitExists) return (false, "This appointment already has a visit.", null);

        var visit = new Visit
        {
            AppointmentId = dto.AppointmentId,
            Symptoms = dto.Symptoms,
            Diagnosis = dto.Diagnosis,
            Notes = dto.Notes
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        if (appointment.Status != AppointmentStatus.Completed)
        {
            appointment.Status = AppointmentStatus.Completed;
            await _context.SaveChangesAsync();
        }

        var created = await GetByIdAsync(visit.Id);
        return (true, "Visit created successfully.", created);
    }

    public async Task<(bool Success, string Message, VisitDto? Visit)> UpdateAsync(int id, UpdateVisitDto dto)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit is null) return (false, "Visit not found.", null);

        if (string.IsNullOrWhiteSpace(dto.Symptoms))
            return (false, "Symptoms are required.", null);

        if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            return (false, "Diagnosis is required.", null);

        visit.Symptoms = dto.Symptoms;
        visit.Diagnosis = dto.Diagnosis;
        visit.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(visit.Id);
        return (true, "Visit updated successfully.", updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit is null) return (false, "Visit not found.");

        var hasPrescriptions = await _context.Prescriptions.AnyAsync(p => p.VisitId == id);
        if (hasPrescriptions)
            return (false, "Cannot delete visit because there are prescriptions linked to this visit.");

        _context.Visits.Remove(visit);
        await _context.SaveChangesAsync();
        return (true, "Visit deleted successfully.");
    }
}