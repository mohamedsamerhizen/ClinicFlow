using System.ComponentModel.DataAnnotations;
using ClinicFlow.DTOs.Common;

namespace ClinicFlow.DTOs.Prescription;

public class PrescriptionQueryParams : PaginationParams
{
    [Range(1, int.MaxValue)]
    public int? VisitId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int? DoctorId { get; set; }

    [StringLength(200)]
    public string? Search { get; set; }
}