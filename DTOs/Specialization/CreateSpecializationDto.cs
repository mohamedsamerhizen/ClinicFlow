using System.ComponentModel.DataAnnotations;

namespace ClinicFlow.DTOs.Specialization;

public class CreateSpecializationDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}