using System.ComponentModel.DataAnnotations;

namespace ClinicFlow.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }
}