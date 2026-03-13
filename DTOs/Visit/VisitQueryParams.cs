using System.ComponentModel.DataAnnotations;
using ClinicFlow.DTOs.Common;

namespace ClinicFlow.DTOs.Visit;

public class VisitQueryParams : PaginationParams
{
    [Range(1, int.MaxValue)]
    public int? DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PatientId { get; set; }

    public DateTime? Date { get; set; }

    [StringLength(200)]
    public string? Search { get; set; }
}