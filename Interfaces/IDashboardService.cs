using ClinicFlow.DTOs.Dashboard;

namespace ClinicFlow.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
