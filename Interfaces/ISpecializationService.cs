using ClinicFlow.DTOs.Specialization;

namespace ClinicFlow.Interfaces;

public interface ISpecializationService
{
    Task<List<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SpecializationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, SpecializationDto? Specialization)> CreateAsync(CreateSpecializationDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, SpecializationDto? Specialization)> UpdateAsync(int id, CreateSpecializationDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
