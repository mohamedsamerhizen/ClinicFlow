using ClinicFlow.Common;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Prescription;
using ClinicFlow.Entities;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _context;

    public PrescriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams)
    {
        var query = _context.Prescriptions.AsNoTracking().AsQueryable();

        if (queryParams.VisitId.HasValue)
            query = query.Where(p => p.VisitId == queryParams.VisitId.Value);

        if (queryParams.PatientId.HasValue)
            query = query.Where(p => p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.PatientId == queryParams.PatientId.Value);

        if (queryParams.DoctorId.HasValue)
            query = query.Where(p => p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.DoctorId == queryParams.DoctorId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim();
            query = query.Where(p =>
                p.MedicationName.Contains(search) ||
                p.Dosage.Contains(search) ||
                p.Instructions.Contains(search) ||
                (p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.Patient != null && p.Visit.Appointment.Patient.FullName.Contains(search)) ||
                (p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.Doctor != null && p.Visit.Appointment.Doctor.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        var items = await query
            .OrderByDescending(p => p.Visit != null && p.Visit.Appointment != null ? p.Visit.Appointment.AppointmentDate : DateTime.MinValue)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                VisitId = p.VisitId,
                AppointmentId = p.Visit != null ? p.Visit.AppointmentId : 0,
                AppointmentDate = p.Visit != null && p.Visit.Appointment != null ? p.Visit.Appointment.AppointmentDate : DateTime.MinValue,
                PatientName = p.Visit != null && p.Visit.Appointment != null
                    ? patientsQuery
                        .Where(pt => pt.Id == p.Visit.Appointment.PatientId)
                        .Select(pt => pt.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = p.Visit != null && p.Visit.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == p.Visit.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Instructions = p.Instructions,
                DurationInDays = p.DurationInDays
            })
            .ToListAsync();

        return new PagedResponse<PrescriptionDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PrescriptionDto?> GetByIdAsync(int id)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        return await _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                VisitId = p.VisitId,
                AppointmentId = p.Visit != null ? p.Visit.AppointmentId : 0,
                AppointmentDate = p.Visit != null && p.Visit.Appointment != null ? p.Visit.Appointment.AppointmentDate : DateTime.MinValue,
                PatientName = p.Visit != null && p.Visit.Appointment != null
                    ? patientsQuery
                        .Where(pt => pt.Id == p.Visit.Appointment.PatientId)
                        .Select(pt => pt.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = p.Visit != null && p.Visit.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == p.Visit.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Instructions = p.Instructions,
                DurationInDays = p.DurationInDays
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, PrescriptionDto? Prescription)> CreateAsync(CreatePrescriptionDto dto)
    {
        var medicationName = dto.MedicationName.Trim();
        var dosage = dto.Dosage.Trim();
        var instructions = dto.Instructions.Trim();

        if (string.IsNullOrWhiteSpace(medicationName))
            return (false, "Medication name is required.", null);

        if (string.IsNullOrWhiteSpace(dosage))
            return (false, "Dosage is required.", null);

        var visitExists = await _context.Visits.AnyAsync(v => v.Id == dto.VisitId);
        if (!visitExists) return (false, "Invalid visit id.", null);

        var prescription = new Prescription
        {
            VisitId = dto.VisitId,
            MedicationName = medicationName,
            Dosage = dosage,
            Instructions = instructions,
            DurationInDays = dto.DurationInDays
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        var created = await GetByIdAsync(prescription.Id);
        return (true, "Prescription created successfully.", created);
    }

    public async Task<(bool Success, string Message, PrescriptionDto? Prescription)> UpdateAsync(int id, UpdatePrescriptionDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription is null) return (false, "Prescription not found.", null);

        var medicationName = dto.MedicationName.Trim();
        var dosage = dto.Dosage.Trim();
        var instructions = dto.Instructions.Trim();

        if (string.IsNullOrWhiteSpace(medicationName))
            return (false, "Medication name is required.", null);

        if (string.IsNullOrWhiteSpace(dosage))
            return (false, "Dosage is required.", null);

        prescription.MedicationName = medicationName;
        prescription.Dosage = dosage;
        prescription.Instructions = instructions;
        prescription.DurationInDays = dto.DurationInDays;

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(prescription.Id);
        return (true, "Prescription updated successfully.", updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription is null) return (false, "Prescription not found.");

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();
        return (true, "Prescription deleted successfully.");
    }
}