namespace ClinicFlow.Entities;

public class Doctor : BaseEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public int SpecializationId { get; set; }
    public Specialization? Specialization { get; set; }
}