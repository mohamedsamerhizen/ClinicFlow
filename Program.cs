using System.Threading.RateLimiting;
using ClinicFlow.Common;
using ClinicFlow.Data;
using ClinicFlow.Data.Seed;
using ClinicFlow.Entities;
using ClinicFlow.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddApplicationValidationResponse();
builder.Services.AddApplicationSwagger();
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCaching();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.FailResponse("Too many requests. Please try again later.");
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AdminSeedOptions>(
    builder.Configuration.GetSection(AdminSeedOptions.SectionName));

builder.Services.Configure<DemoSeedOptions>(
    builder.Configuration.GetSection(DemoSeedOptions.SectionName));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/", () =>
    Results.Ok(ApiResponse<string>.SuccessResponse("ClinicFlow API is running.")))
    .AllowAnonymous()
    .WithName("Root");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var adminSeedOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>();
    var demoSeedOptions = services.GetRequiredService<IOptions<DemoSeedOptions>>();

    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAdminAsync(userManager, roleManager, adminSeedOptions);
    await DemoDataSeeder.SeedAsync(dbContext, demoSeedOptions);
}

app.Run();