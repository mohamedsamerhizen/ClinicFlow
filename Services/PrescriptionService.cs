using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Prescription;
using ClinicFlow.Entities;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PrescriptionService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .AsQueryable();

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            if (!currentDoctorId.HasValue)
            {
                return new PagedResponse<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    TotalCount = 0
                };
            }

            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == currentDoctorId.Value);
        }
        else if (queryParams.DoctorId.HasValue)
        {
            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == queryParams.DoctorId.Value);
        }

        if (queryParams.VisitId.HasValue)
            query = query.Where(p => p.VisitId == queryParams.VisitId.Value);

        if (queryParams.PatientId.HasValue)
            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.PatientId == queryParams.PatientId.Value);

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
                AppointmentDate = p.Visit != null && p.Visit.Appointment != null
                    ? p.Visit.Appointment.AppointmentDate
                    : DateTime.MinValue,
                PatientName = p.Visit != null && p.Visit.Appointment != null
                    ? patientsQuery.Where(pt => pt.Id == p.Visit.Appointment.PatientId).Select(pt => pt.FullName).FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = p.Visit != null && p.Visit.Appointment != null
                    ? doctorsQuery.Where(d => d.Id == p.Visit.Appointment.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty
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

    public async Task<PrescriptionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == id);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            if (!currentDoctorId.HasValue)
                return null;

            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == currentDoctorId.Value);
        }

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        return await query
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                VisitId = p.VisitId,
                AppointmentId = p.Visit != null ? p.Visit.AppointmentId : 0,
                AppointmentDate = p.Visit != null && p.Visit.Appointment != null
                    ? p.Visit.Appointment.AppointmentDate
                    : DateTime.MinValue,
                PatientName = p.Visit != null && p.Visit.Appointment != null
                    ? patientsQuery.Where(pt => pt.Id == p.Visit.Appointment.PatientId).Select(pt => pt.FullName).FirstOrDefault() ?? string.Empty
                    : string.Empty,
                DoctorName = p.Visit != null && p.Visit.Appointment != null
                    ? doctorsQuery.Where(d => d.Id == p.Visit.Appointment.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty
                    : string.Empty,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Instructions = p.Instructions,
                DurationInDays = p.DurationInDays
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, PrescriptionDto? Prescription)> CreateAsync(CreatePrescriptionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.MedicationName))
            return (false, "Medication name is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Dosage))
            return (false, "Dosage is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Instructions))
            return (false, "Instructions are required.", null);

        var visit = await _context.Visits
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.Id == dto.VisitId);

        if (visit is null)
            return (false, "Invalid visit id.", null);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            if (!currentDoctorId.HasValue || visit.Appointment?.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to create a prescription for this visit.", null);
        }

        var prescription = new Prescription
        {
            VisitId = dto.VisitId,
            MedicationName = dto.MedicationName.Trim(),
            Dosage = dto.Dosage.Trim(),
            Instructions = dto.Instructions.Trim(),
            DurationInDays = dto.DurationInDays
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        var created = await GetByIdAsync(prescription.Id);
        return (true, "Prescription created successfully.", created);
    }

    public async Task<(bool Success, string Message, PrescriptionDto? Prescription)> UpdateAsync(int id, UpdatePrescriptionDto dto, CancellationToken cancellationToken = default)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription is null)
            return (false, "Prescription not found.", null);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            if (!currentDoctorId.HasValue || prescription.Visit?.Appointment?.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to update this prescription.", null);
        }

        if (string.IsNullOrWhiteSpace(dto.MedicationName))
            return (false, "Medication name is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Dosage))
            return (false, "Dosage is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Instructions))
            return (false, "Instructions are required.", null);

        prescription.MedicationName = dto.MedicationName.Trim();
        prescription.Dosage = dto.Dosage.Trim();
        prescription.Instructions = dto.Instructions.Trim();
        prescription.DurationInDays = dto.DurationInDays;

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(prescription.Id);
        return (true, "Prescription updated successfully.", updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription is null)
            return (false, "Prescription not found.");

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            if (!currentDoctorId.HasValue || prescription.Visit?.Appointment?.DoctorId != currentDoctorId.Value)
                return (false, "You are not allowed to delete this prescription.");
        }

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();

        return (true, "Prescription deleted successfully.");
    }

    private bool IsDoctor()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(AppRoles.Doctor) == true;
    }

    private async Task<int?> GetCurrentDoctorIdAsync()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.ApplicationUserId == userId)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();
    }
}
