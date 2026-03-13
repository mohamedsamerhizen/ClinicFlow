using ClinicFlow.DTOs.Specialization;

namespace ClinicFlow.Interfaces;

public interface ISpecializationService
{
    Task<List<SpecializationDto>> GetAllAsync();
    Task<SpecializationDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, SpecializationDto? Specialization)> CreateAsync(CreateSpecializationDto dto);
    Task<(bool Success, string Message, SpecializationDto? Specialization)> UpdateAsync(int id, CreateSpecializationDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}