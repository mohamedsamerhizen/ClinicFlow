using ClinicFlow.Entities;
using ClinicFlow.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClinicFlow.Data.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, IOptions<DemoSeedOptions> demoSeedOptions)
    {
        if (!demoSeedOptions.Value.Enabled)
            return;

        await SeedSpecializationsAsync(context);
        await SeedDoctorsAsync(context);
        await SeedPatientsAsync(context);
        await SeedDoctorSchedulesAsync(context);
        await SeedAppointmentsAsync(context);
        await SeedVisitsAndPrescriptionsAsync(context);
    }

    private static async Task SeedSpecializationsAsync(AppDbContext context)
    {
        if (await context.Specializations.AnyAsync())
            return;

        var specializations = new List<Specialization>
        {
            new() { Name = "Cardiology" },
            new() { Name = "Dermatology" },
            new() { Name = "Pediatrics" }
        };

        context.Specializations.AddRange(specializations);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDoctorsAsync(AppDbContext context)
    {
        if (await context.Doctors.AnyAsync())
            return;

        var cardiologyId = await context.Specializations
            .Where(s => s.Name == "Cardiology")
            .Select(s => s.Id)
            .FirstAsync();

        var dermatologyId = await context.Specializations
            .Where(s => s.Name == "Dermatology")
            .Select(s => s.Id)
            .FirstAsync();

        var pediatricsId = await context.Specializations
            .Where(s => s.Name == "Pediatrics")
            .Select(s => s.Id)
            .FirstAsync();

        var doctors = new List<Doctor>
        {
            new()
            {
                FullName = "Dr. Ahmed Ali",
                PhoneNumber = "07712345678",
                SpecializationId = cardiologyId
            },
            new()
            {
                FullName = "Dr. Sara Hassan",
                PhoneNumber = "07812345678",
                SpecializationId = dermatologyId
            },
            new()
            {
                FullName = "Dr. Noor Kareem",
                PhoneNumber = "07912345678",
                SpecializationId = pediatricsId
            }
        };

        context.Doctors.AddRange(doctors);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPatientsAsync(AppDbContext context)
    {
        if (await context.Patients.AnyAsync())
            return;

        var patients = new List<Patient>
        {
            new()
            {
                FullName = "Mohammed Samer",
                PhoneNumber = "07512345678",
                DateOfBirth = new DateTime(2003, 5, 10),
                Gender = "Male"
            },
            new()
            {
                FullName = "Zainab Ali",
                PhoneNumber = "07522345678",
                DateOfBirth = new DateTime(1999, 11, 20),
                Gender = "Female"
            },
            new()
            {
                FullName = "Omar Hassan",
                PhoneNumber = "07532345678",
                DateOfBirth = new DateTime(2015, 3, 15),
                Gender = "Male"
            },
            new()
            {
                FullName = "Fatima Kareem",
                PhoneNumber = "07542345678",
                DateOfBirth = new DateTime(1988, 7, 2),
                Gender = "Female"
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDoctorSchedulesAsync(AppDbContext context)
    {
        if (await context.DoctorSchedules.AnyAsync())
            return;

        var drAhmedId = await context.Doctors
            .Where(d => d.FullName == "Dr. Ahmed Ali")
            .Select(d => d.Id)
            .FirstAsync();

        var drSaraId = await context.Doctors
            .Where(d => d.FullName == "Dr. Sara Hassan")
            .Select(d => d.Id)
            .FirstAsync();

        var drNoorId = await context.Doctors
            .Where(d => d.FullName == "Dr. Noor Kareem")
            .Select(d => d.Id)
            .FirstAsync();

        var schedules = new List<DoctorSchedule>
        {
            new() { DoctorId = drAhmedId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(13, 0, 0) },
            new() { DoctorId = drAhmedId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(20, 0, 0) },
            new() { DoctorId = drAhmedId, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(13, 0, 0) },

            new() { DoctorId = drSaraId, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0) },
            new() { DoctorId = drSaraId, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0) },

            new() { DoctorId = drNoorId, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0) },
            new() { DoctorId = drNoorId, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0) }
        };

        context.DoctorSchedules.AddRange(schedules);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAppointmentsAsync(AppDbContext context)
    {
        if (await context.Appointments.AnyAsync())
            return;

        var drAhmedId = await context.Doctors
            .Where(d => d.FullName == "Dr. Ahmed Ali")
            .Select(d => d.Id)
            .FirstAsync();

        var drSaraId = await context.Doctors
            .Where(d => d.FullName == "Dr. Sara Hassan")
            .Select(d => d.Id)
            .FirstAsync();

        var drNoorId = await context.Doctors
            .Where(d => d.FullName == "Dr. Noor Kareem")
            .Select(d => d.Id)
            .FirstAsync();

        var mohammedId = await context.Patients
            .Where(p => p.FullName == "Mohammed Samer")
            .Select(p => p.Id)
            .FirstAsync();

        var zainabId = await context.Patients
            .Where(p => p.FullName == "Zainab Ali")
            .Select(p => p.Id)
            .FirstAsync();

        var omarId = await context.Patients
            .Where(p => p.FullName == "Omar Hassan")
            .Select(p => p.Id)
            .FirstAsync();

        var fatimaId = await context.Patients
            .Where(p => p.FullName == "Fatima Kareem")
            .Select(p => p.Id)
            .FirstAsync();

        var appointments = new List<Appointment>
        {
            new()
            {
                DoctorId = drAhmedId,
                PatientId = mohammedId,
                AppointmentDate = GetNextWeekdayAt(DayOfWeek.Monday, 9, 0),
                Status = AppointmentStatus.Pending
            },
            new()
            {
                DoctorId = drAhmedId,
                PatientId = zainabId,
                AppointmentDate = GetNextWeekdayAt(DayOfWeek.Monday, 9, 15),
                Status = AppointmentStatus.Confirmed
            },
            new()
            {
                DoctorId = drAhmedId,
                PatientId = fatimaId,
                AppointmentDate = GetNextWeekdayAt(DayOfWeek.Monday, 16, 0),
                Status = AppointmentStatus.Cancelled
            },
            new()
            {
                DoctorId = drSaraId,
                PatientId = fatimaId,
                AppointmentDate = GetNextWeekdayAt(DayOfWeek.Tuesday, 10, 0),
                Status = AppointmentStatus.Pending
            },
            new()
            {
                DoctorId = drNoorId,
                PatientId = omarId,
                AppointmentDate = GetNextWeekdayAt(DayOfWeek.Sunday, 8, 0),
                Status = AppointmentStatus.Confirmed
            },
            new()
            {
                DoctorId = drAhmedId,
                PatientId = mohammedId,
                AppointmentDate = GetPreviousWeekdayAt(DayOfWeek.Wednesday, 9, 0),
                Status = AppointmentStatus.Completed
            }
        };

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedVisitsAndPrescriptionsAsync(AppDbContext context)
    {
        if (await context.Visits.AnyAsync() || await context.Prescriptions.AnyAsync())
            return;

        var completedAppointmentId = await context.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        if (completedAppointmentId == 0)
            return;

        var visit = new Visit
        {
            AppointmentId = completedAppointmentId,
            Symptoms = "Chest discomfort and shortness of breath.",
            Diagnosis = "Mild hypertension with stress-related symptoms.",
            Notes = "Patient advised to reduce salt intake and monitor blood pressure regularly."
        };

        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        var prescriptions = new List<Prescription>
        {
            new()
            {
                VisitId = visit.Id,
                MedicationName = "Amlodipine",
                Dosage = "5 mg once daily",
                Instructions = "Take after breakfast.",
                DurationInDays = 30
            },
            new()
            {
                VisitId = visit.Id,
                MedicationName = "Vitamin D",
                Dosage = "1000 IU once daily",
                Instructions = "Take in the morning.",
                DurationInDays = 30
            }
        };

        context.Prescriptions.AddRange(prescriptions);
        await context.SaveChangesAsync();
    }

    private static DateTime GetNextWeekdayAt(DayOfWeek dayOfWeek, int hour, int minute)
    {
        var baseDate = DateTime.Today;
        var daysToAdd = ((int)dayOfWeek - (int)baseDate.DayOfWeek + 7) % 7;

        if (daysToAdd == 0)
            daysToAdd = 7;

        return baseDate.AddDays(daysToAdd).AddHours(hour).AddMinutes(minute);
    }

    private static DateTime GetPreviousWeekdayAt(DayOfWeek dayOfWeek, int hour, int minute)
    {
        var baseDate = DateTime.Today;
        var daysBack = ((int)baseDate.DayOfWeek - (int)dayOfWeek + 7) % 7;

        if (daysBack == 0)
            daysBack = 7;

        return baseDate.AddDays(-daysBack).AddHours(hour).AddMinutes(minute);
    }
}