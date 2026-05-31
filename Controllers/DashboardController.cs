using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.None, NoStore = false)]
    public async Task<IActionResult> GetOverview()
    {
        var overview = await _dashboardService.GetOverviewAsync();
        return Ok(ApiResponse<object>.SuccessResponse(overview));
    }
}
