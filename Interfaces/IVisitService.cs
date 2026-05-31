using ClinicFlow.Common;
using ClinicFlow.DTOs.Visit;

namespace ClinicFlow.Interfaces;

public interface IVisitService
{
    Task<PagedResponse<VisitDto>> GetAllAsync(VisitQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, VisitDto? Visit)> CreateAsync(CreateVisitDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, VisitDto? Visit)> UpdateAsync(int id, UpdateVisitDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
