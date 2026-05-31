using ClinicFlow.DTOs.Visit;
using ClinicFlow.Enums;
using ClinicFlow.Tests.TestSupport;

namespace ClinicFlow.Tests;

public class VisitServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentHasNotHappenedYet()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var futureAppointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed);

        context.Appointments.Add(futureAppointment);
        await context.SaveChangesAsync();

        var service = ClinicTestFactory.CreateVisitService(context);

        var result = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = futureAppointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });

        Assert.False(result.Success);
        Assert.Equal("Cannot create visit before appointment time.", result.Message);
    }
}
