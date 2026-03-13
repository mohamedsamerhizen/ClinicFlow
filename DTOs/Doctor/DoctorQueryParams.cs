using System.ComponentModel.DataAnnotations;
using ClinicFlow.DTOs.Common;

namespace ClinicFlow.DTOs.Doctor;

public class DoctorQueryParams : PaginationParams
{
    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? SpecializationId { get; set; }
}