using Microsoft.AspNetCore.Identity;

namespace ClinicFlow.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    public Doctor? DoctorProfile { get; set; }
}
