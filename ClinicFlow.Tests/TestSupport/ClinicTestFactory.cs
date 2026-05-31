using System.Security.Claims;
using ClinicFlow.Constants;
using ClinicFlow.Data;
using ClinicFlow.DTOs.Appointment;
using ClinicFlow.Entities;
using ClinicFlow.Enums;
using ClinicFlow.Interfaces;
using ClinicFlow.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Tests.TestSupport;

internal static class ClinicTestFactory
{
    public const string DoctorUserId = "doctor-user-1";
    public const string OtherDoctorUserId = "doctor-user-2";

    public static AppDbContext CreateContext(string? currentUserId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new TestCurrentUserService { UserId = currentUserId });
    }

    public static AppointmentService CreateAppointmentService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new AppointmentService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static VisitService CreateVisitService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new VisitService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static async Task<SeededClinic> SeedClinicAsync(AppDbContext context)
    {
        var specialization = new Specialization { Name = "Cardiology" };

        var doctorUser = new ApplicationUser
        {
            Id = DoctorUserId,
            FullName = "Dr. Ahmed Ali",
            Email = "doctor1@example.com",
            UserName = "doctor1@example.com",
            EmailConfirmed = true
        };

        var otherDoctorUser = new ApplicationUser
        {
            Id = OtherDoctorUserId,
            FullName = "Dr. Sara Hassan",
            Email = "doctor2@example.com",
            UserName = "doctor2@example.com",
            EmailConfirmed = true
        };

        var doctor = new Doctor
        {
            FullName = "Dr. Ahmed Ali",
            PhoneNumber = "07712345678",
            ApplicationUserId = DoctorUserId,
            Specialization = specialization
        };

        var otherDoctor = new Doctor
        {
            FullName = "Dr. Sara Hassan",
            PhoneNumber = "07812345678",
            ApplicationUserId = OtherDoctorUserId,
            Specialization = specialization
        };

        var patient = new Patient
        {
            FullName = "Mohammed Samer",
            PhoneNumber = "07512345678",
            DateOfBirth = new DateTime(2003, 5, 10),
            Gender = "Male"
        };

        var otherPatient = new Patient
        {
            FullName = "Zainab Ali",
            PhoneNumber = "07522345678",
            DateOfBirth = new DateTime(1999, 11, 20),
            Gender = "Female"
        };

        context.Users.AddRange(doctorUser, otherDoctorUser);
        context.AddRange(specialization, doctor, otherDoctor, patient, otherPatient);
        await context.SaveChangesAsync();

        context.DoctorSchedules.AddRange(
            new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            },
            new DoctorSchedule
            {
                DoctorId = otherDoctor.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });

        await context.SaveChangesAsync();

        return new SeededClinic(doctor, otherDoctor, patient, otherPatient);
    }

    public static DateTime NextMondayAt(int hour, int minute)
    {
        var today = DateTime.Today;
        var target = today.AddDays(((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7)
            .AddHours(hour)
            .AddMinutes(minute);

        return target <= DateTime.Now ? target.AddDays(7) : target;
    }

    public static CreateAppointmentDto CreateAppointmentDto(
        SeededClinic seeded,
        DateTime appointmentDate)
    {
        return new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = appointmentDate
        };
    }

    public static Appointment CreateAppointment(
        Doctor doctor,
        Patient patient,
        DateTime appointmentDate,
        AppointmentStatus status = AppointmentStatus.Pending)
    {
        return new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = appointmentDate,
            Status = status
        };
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? userId, params string[] roles)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        foreach (var role in roles.DefaultIfEmpty(AppRoles.Admin))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}

internal sealed record SeededClinic(
    Doctor Doctor,
    Doctor OtherDoctor,
    Patient Patient,
    Patient OtherPatient);
