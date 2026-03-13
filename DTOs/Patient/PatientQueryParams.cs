using System.ComponentModel.DataAnnotations;
using ClinicFlow.DTOs.Common;

namespace ClinicFlow.DTOs.Patient;

public class PatientQueryParams : PaginationParams
{
    [StringLength(100)]
    public string? Search { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}