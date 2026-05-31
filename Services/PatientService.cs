using ClinicFlow.Common;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Patient;
using ClinicFlow.Entities;
using ClinicFlow.Enums;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _context;

    public PatientService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<PatientDto>> GetAllAsync(PatientQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(search) ||
                p.PhoneNumber.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Gender))
        {
            var gender = queryParams.Gender.Trim().ToLower();
            query = query.Where(p => p.Gender.ToLower() == gender);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.FullName)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .ToListAsync();

        return new PagedResponse<PatientDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<PatientDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var search = name.Trim().ToLower();

        return await _context.Patients
            .AsNoTracking()
            .Where(p => p.FullName.ToLower().Contains(search))
            .OrderBy(p => p.FullName)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .ToListAsync();
    }

    public async Task<PatientHistoryDto?> GetHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null) return null;

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var totalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id);

        var visits = await _context.Visits
            .AsNoTracking()
            .Where(v => v.Appointment != null && v.Appointment.PatientId == id)
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .Select(v => new PatientHistoryVisitDto
            {
                VisitId = v.Id,
                AppointmentId = v.AppointmentId,
                AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
                DoctorName = v.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == v.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Symptoms = v.Symptoms,
                Diagnosis = v.Diagnosis,
                Notes = v.Notes,
                Prescriptions = v.Prescriptions
                    .Select(p => new PatientHistoryPrescriptionDto
                    {
                        PrescriptionId = p.Id,
                        MedicationName = p.MedicationName,
                        Dosage = p.Dosage,
                        Instructions = p.Instructions,
                        DurationInDays = p.DurationInDays
                    })
                    .ToList()
            })
            .ToListAsync();

        return new PatientHistoryDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            TotalAppointments = totalAppointments,
            TotalVisits = visits.Count,
            LastVisitDate = visits.FirstOrDefault()?.AppointmentDate,
            Visits = visits
        };
    }

    public async Task<PatientSummaryDto?> GetSummaryAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PatientSummaryDto
            {
                PatientId = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .FirstOrDefaultAsync();

        if (patient is null)
            return null;

        patient.TotalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id);
        patient.PendingAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id && a.Status == AppointmentStatus.Pending);
        patient.ConfirmedAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id && a.Status == AppointmentStatus.Confirmed);
        patient.CompletedAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id && a.Status == AppointmentStatus.Completed);
        patient.CancelledAppointments = await _context.Appointments.CountAsync(a => a.PatientId == id && a.Status == AppointmentStatus.Cancelled);
        patient.TotalVisits = await _context.Visits.CountAsync(v => v.Appointment != null && v.Appointment.PatientId == id);
        patient.TotalPrescriptions = await _context.Prescriptions.CountAsync(p => p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.PatientId == id);
        patient.LastAppointmentDate = await _context.Appointments
            .Where(a => a.PatientId == id)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => (DateTime?)a.AppointmentDate)
            .FirstOrDefaultAsync();

        patient.LastVisitDate = await _context.Visits
            .Where(v => v.Appointment != null && v.Appointment.PatientId == id)
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .Select(v => (DateTime?)(v.Appointment != null ? v.Appointment.AppointmentDate : null))
            .FirstOrDefaultAsync();

        return patient;
    }

    public async Task<(bool Success, string Message, PatientDto? Patient)> CreateAsync(CreatePatientDto dto, CancellationToken cancellationToken = default)
    {
        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var gender = dto.Gender.Trim();
        var today = DateTime.Today;
        var minimumAllowedDate = today.AddYears(-130);

        if (dto.DateOfBirth.Date > today)
            return (false, "Date of birth cannot be in the future.", null);

        if (dto.DateOfBirth.Date < minimumAllowedDate)
            return (false, "Date of birth is outside the allowed range.", null);

        var phoneExists = await _context.Patients
            .AnyAsync(p => p.PhoneNumber.ToLower() == phoneNumber.ToLower());

        if (phoneExists)
            return (false, "Patient phone number already exists.", null);

        var patient = new Patient
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dto.DateOfBirth.Date,
            Gender = gender
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return (true, "Patient created successfully.", await GetByIdAsync(patient.Id));
    }

    public async Task<(bool Success, string Message, PatientDto? Patient)> UpdateAsync(int id, CreatePatientDto dto, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient is null)
            return (false, "Patient not found.", null);

        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var gender = dto.Gender.Trim();
        var today = DateTime.Today;
        var minimumAllowedDate = today.AddYears(-130);

        if (dto.DateOfBirth.Date > today)
            return (false, "Date of birth cannot be in the future.", null);

        if (dto.DateOfBirth.Date < minimumAllowedDate)
            return (false, "Date of birth is outside the allowed range.", null);

        var phoneExists = await _context.Patients
            .AnyAsync(p => p.Id != id && p.PhoneNumber.ToLower() == phoneNumber.ToLower());

        if (phoneExists)
            return (false, "Patient phone number already exists.", null);

        patient.FullName = fullName;
        patient.PhoneNumber = phoneNumber;
        patient.DateOfBirth = dto.DateOfBirth.Date;
        patient.Gender = gender;

        await _context.SaveChangesAsync();
        return (true, "Patient updated successfully.", await GetByIdAsync(patient.Id));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient is null) return (false, "Patient not found.");

        var hasAppointments = await _context.Appointments.AnyAsync(a => a.PatientId == id);
        if (hasAppointments)
            return (false, "Cannot delete patient because there are appointments linked to this patient.");

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();
        return (true, "Patient deleted successfully.");
    }
}
