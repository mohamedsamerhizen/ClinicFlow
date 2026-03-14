using Microsoft.AspNetCore.Identity;

namespace ClinicFlow.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Doctor? DoctorProfile { get; set; }
}