using ClinicFlow.Interfaces;
using ClinicFlow.Services;

namespace ClinicFlow.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ISpecializationService, SpecializationService>();
        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}