using ClinicFlow.Common;
using ClinicFlow.DTOs.Visit;

namespace ClinicFlow.Interfaces;

public interface IVisitService
{
    Task<PagedResponse<VisitDto>> GetAllAsync(VisitQueryParams queryParams);
    Task<VisitDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, VisitDto? Visit)> CreateAsync(CreateVisitDto dto);
    Task<(bool Success, string Message, VisitDto? Visit)> UpdateAsync(int id, UpdateVisitDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}