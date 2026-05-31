using ClinicFlow.Constants;
using ClinicFlow.Controllers;
using ClinicFlow.DTOs.Appointment;
using Microsoft.AspNetCore.Authorization;

namespace ClinicFlow.Tests;

public class AuthorizationMetadataTests
{
    [Fact]
    public void AppointmentsCreate_RequiresAdminOrReceptionistPolicy()
    {
        var method = typeof(AppointmentsController).GetMethod(
            nameof(AppointmentsController.Create),
            new[] { typeof(CreateAppointmentDto) });

        Assert.NotNull(method);

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AppPolicies.AdminOrReceptionist, authorizeAttribute.Policy);
    }
}
