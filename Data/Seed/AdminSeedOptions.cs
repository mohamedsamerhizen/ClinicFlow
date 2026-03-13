
namespace ClinicFlow.Data.Seed;

public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string FullName { get; set; } = "System Administrator";
    public string Email { get; set; } = "admin@clinicflow.com";
    public string Password { get; set; } = "Admin@123";
}