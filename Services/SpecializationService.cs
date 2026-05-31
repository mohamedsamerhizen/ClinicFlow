using ClinicFlow.Data;
using ClinicFlow.DTOs.Specialization;
using ClinicFlow.Entities;
using ClinicFlow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Services;

public class SpecializationService : ISpecializationService
{
    private readonly AppDbContext _context;

    public SpecializationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();
    }

    public async Task<SpecializationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .Where(s => s.Id == id)
            .Select(s => new SpecializationDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, SpecializationDto? Specialization)> CreateAsync(CreateSpecializationDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();

        var exists = await _context.Specializations
            .AnyAsync(s => s.Name.ToLower() == name.ToLower());

        if (exists)
            return (false, "Specialization name already exists.", null);

        var specialization = new Specialization
        {
            Name = name
        };

        _context.Specializations.Add(specialization);
        await _context.SaveChangesAsync();

        return (true, "Specialization created successfully.", new SpecializationDto
        {
            Id = specialization.Id,
            Name = specialization.Name
        });
    }

    public async Task<(bool Success, string Message, SpecializationDto? Specialization)> UpdateAsync(int id, CreateSpecializationDto dto, CancellationToken cancellationToken = default)
    {
        var specialization = await _context.Specializations.FindAsync(id);
        if (specialization is null)
            return (false, "Specialization not found.", null);

        var name = dto.Name.Trim();

        var exists = await _context.Specializations
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower());

        if (exists)
            return (false, "Specialization name already exists.", null);

        specialization.Name = name;
        await _context.SaveChangesAsync();

        return (true, "Specialization updated successfully.", new SpecializationDto
        {
            Id = specialization.Id,
            Name = specialization.Name
        });
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialization = await _context.Specializations.FindAsync(id);
        if (specialization is null)
            return (false, "Specialization not found.");

        var hasDoctors = await _context.Doctors.AnyAsync(d => d.SpecializationId == id);
        if (hasDoctors)
            return (false, "Cannot delete specialization because there are doctors linked to this specialization.");

        _context.Specializations.Remove(specialization);
        await _context.SaveChangesAsync();

        return (true, "Specialization deleted successfully.");
    }
}
