using ClinicFlow.Common;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Doctor;
using ClinicFlow.Entities;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _context;

    public DoctorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<DoctorDto>> GetAllAsync(DoctorQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Doctors
            .Include(d => d.Specialization)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();

            query = query.Where(d =>
                d.FullName.ToLower().Contains(search) ||
                d.PhoneNumber.ToLower().Contains(search) ||
                (d.Specialization != null && d.Specialization.Name.ToLower().Contains(search)));
        }

        if (queryParams.SpecializationId.HasValue)
        {
            query = query.Where(d => d.SpecializationId == queryParams.SpecializationId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.FullName)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.FullName,
                PhoneNumber = d.PhoneNumber,
                SpecializationId = d.SpecializationId,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .ToListAsync();

        return new PagedResponse<DoctorDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DoctorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .Include(d => d.Specialization)
            .Where(d => d.Id == id)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.FullName,
                PhoneNumber = d.PhoneNumber,
                SpecializationId = d.SpecializationId,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, DoctorDto? Doctor)> CreateAsync(CreateDoctorDto dto, CancellationToken cancellationToken = default)
    {
        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var applicationUserId = string.IsNullOrWhiteSpace(dto.ApplicationUserId)
            ? null
            : dto.ApplicationUserId.Trim();

        var specializationExists = await _context.Specializations.AnyAsync(s => s.Id == dto.SpecializationId);
        if (!specializationExists)
            return (false, "Invalid specialization id.", null);

        var phoneExists = await _context.Doctors
            .AnyAsync(d => d.PhoneNumber.ToLower() == phoneNumber.ToLower());
        if (phoneExists)
            return (false, "Doctor phone number already exists.", null);

        if (!string.IsNullOrWhiteSpace(applicationUserId))
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == applicationUserId);
            if (!userExists)
                return (false, "Invalid application user id.", null);

            var userAlreadyLinked = await _context.Doctors
                .AnyAsync(d => d.ApplicationUserId == applicationUserId);
            if (userAlreadyLinked)
                return (false, "This user is already linked to another doctor.", null);
        }

        var doctor = new Doctor
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            SpecializationId = dto.SpecializationId,
            ApplicationUserId = applicationUserId
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        var createdDoctor = await GetByIdAsync(doctor.Id);
        return (true, "Doctor created successfully.", createdDoctor);
    }

    public async Task<(bool Success, string Message, DoctorDto? Doctor)> UpdateAsync(int id, CreateDoctorDto dto, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor is null)
            return (false, "Doctor not found.", null);

        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var applicationUserId = string.IsNullOrWhiteSpace(dto.ApplicationUserId)
            ? null
            : dto.ApplicationUserId.Trim();

        var specializationExists = await _context.Specializations.AnyAsync(s => s.Id == dto.SpecializationId);
        if (!specializationExists)
            return (false, "Invalid specialization id.", null);

        var phoneExists = await _context.Doctors
            .AnyAsync(d => d.Id != id && d.PhoneNumber.ToLower() == phoneNumber.ToLower());
        if (phoneExists)
            return (false, "Doctor phone number already exists.", null);

        if (!string.IsNullOrWhiteSpace(applicationUserId))
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == applicationUserId);
            if (!userExists)
                return (false, "Invalid application user id.", null);

            var userAlreadyLinked = await _context.Doctors
                .AnyAsync(d => d.Id != id && d.ApplicationUserId == applicationUserId);
            if (userAlreadyLinked)
                return (false, "This user is already linked to another doctor.", null);
        }

        doctor.FullName = fullName;
        doctor.PhoneNumber = phoneNumber;
        doctor.SpecializationId = dto.SpecializationId;
        doctor.ApplicationUserId = applicationUserId;

        await _context.SaveChangesAsync();

        var updatedDoctor = await GetByIdAsync(doctor.Id);
        return (true, "Doctor updated successfully.", updatedDoctor);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor is null)
            return (false, "Doctor not found.");

        var hasSchedules = await _context.DoctorSchedules.AnyAsync(s => s.DoctorId == id);
        if (hasSchedules)
            return (false, "Cannot delete doctor because there are schedules linked to this doctor.");

        var hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id);
        if (hasAppointments)
            return (false, "Cannot delete doctor because there are appointments linked to this doctor.");

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();

        return (true, "Doctor deleted successfully.");
    }
}
