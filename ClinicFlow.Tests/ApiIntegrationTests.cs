using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicFlow.Constants;
using ClinicFlow.DTOs.Appointment;
using ClinicFlow.Enums;
using ClinicFlow.Tests.TestSupport;

namespace ClinicFlow.Tests;

public class ApiIntegrationTests
{
    [Fact]
    public async Task Root_ReturnsRunningMessage()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("ClinicFlow API is running.", json.RootElement.GetProperty("data").GetString());
    }

    [Fact]
    public async Task Health_ReturnsOkWithoutAuthentication()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Appointments_ReturnsUnauthorized_WhenUserIsAnonymous()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VersionedAppointmentsRoute_ReturnsOk_WhenUserIsClinicStaff()
    {
        using var factory = new ClinicWebApplicationFactory(
            "admin-user",
            AppRoles.Admin);
        await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/appointments");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateAppointment_ReturnsForbidden_WhenUserIsDoctor()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var seeded = await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            ClinicTestFactory.CreateAppointmentDto(
                seeded,
                ClinicTestFactory.NextMondayAt(9, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Appointments_ReturnsOnlyCurrentDoctorAppointments_WhenUserIsDoctor()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var seeded = await factory.SeedClinicAsync();

        await factory.ExecuteDbContextAsync(async context =>
        {
            context.Appointments.AddRange(
                ClinicTestFactory.CreateAppointment(
                    seeded.Doctor,
                    seeded.Patient,
                    ClinicTestFactory.NextMondayAt(9, 0),
                    AppointmentStatus.Confirmed),
                ClinicTestFactory.CreateAppointment(
                    seeded.OtherDoctor,
                    seeded.OtherPatient,
                    ClinicTestFactory.NextMondayAt(9, 15),
                    AppointmentStatus.Confirmed));

            await context.SaveChangesAsync();
        });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/appointments");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("data").GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(seeded.Doctor.Id, items[0].GetProperty("doctorId").GetInt32());
    }

    [Fact]
    public async Task CreateAppointment_ReturnsCreated_WhenReceptionistSendsValidRequest()
    {
        using var factory = new ClinicWebApplicationFactory(
            "receptionist-user",
            AppRoles.Receptionist);
        var seeded = await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            new CreateAppointmentDto
            {
                DoctorId = seeded.Doctor.Id,
                PatientId = seeded.Patient.Id,
                AppointmentDate = ClinicTestFactory.NextMondayAt(9, 0)
            });

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Pending", json.RootElement.GetProperty("data").GetProperty("status").GetString());
    }
}
