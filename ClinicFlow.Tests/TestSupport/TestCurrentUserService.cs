using ClinicFlow.Interfaces;

namespace ClinicFlow.Tests.TestSupport;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; init; }
}
