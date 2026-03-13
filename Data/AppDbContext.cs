using ClinicFlow.Entities;
using ClinicFlow.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Visit> Visits { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DoctorSchedule>()
            .HasOne(s => s.Doctor)
            .WithMany()
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Doctor>()
            .HasOne(d => d.Specialization)
            .WithMany()
            .HasForeignKey(d => d.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Visit>()
            .HasOne(v => v.Appointment)
            .WithMany()
            .HasForeignKey(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Prescription>()
            .HasOne(p => p.Visit)
            .WithMany(v => v.Prescriptions)
            .HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Specialization>()
            .HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<Doctor>()
            .HasIndex(d => d.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<Patient>()
            .HasIndex(p => p.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<Appointment>()
            .HasIndex(a => a.DoctorId);

        builder.Entity<Appointment>()
            .HasIndex(a => a.PatientId);

        builder.Entity<Appointment>()
            .HasIndex(a => a.AppointmentDate);

        builder.Entity<Visit>()
            .HasIndex(v => v.AppointmentId)
            .IsUnique();

        builder.Entity<Prescription>()
            .HasIndex(p => p.VisitId);

        builder.Entity<DoctorSchedule>()
            .HasIndex(s => new { s.DoctorId, s.DayOfWeek });

        builder.Entity<Specialization>()
            .Property(s => s.Name)
            .HasMaxLength(100);

        builder.Entity<Doctor>()
            .Property(d => d.FullName)
            .HasMaxLength(100);

        builder.Entity<Doctor>()
            .Property(d => d.PhoneNumber)
            .HasMaxLength(20);

        builder.Entity<Patient>()
            .Property(p => p.FullName)
            .HasMaxLength(100);

        builder.Entity<Patient>()
            .Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        builder.Entity<Patient>()
            .Property(p => p.Gender)
            .HasMaxLength(20);

        builder.Entity<Prescription>()
            .Property(p => p.MedicationName)
            .HasMaxLength(200);

        builder.Entity<Prescription>()
            .Property(p => p.Dosage)
            .HasMaxLength(200);

        builder.Entity<Prescription>()
            .Property(p => p.Instructions)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Symptoms)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Diagnosis)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Entity<Specialization>()
            .HasQueryFilter(s => !s.IsDeleted);

        builder.Entity<Doctor>()
            .HasQueryFilter(d => !d.IsDeleted && !d.Specialization!.IsDeleted);

        builder.Entity<Patient>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Appointment>()
            .HasQueryFilter(a => !a.Doctor!.IsDeleted && !a.Patient!.IsDeleted);

        builder.Entity<DoctorSchedule>()
            .HasQueryFilter(s => !s.Doctor!.IsDeleted);

        builder.Entity<Visit>()
            .HasQueryFilter(v => !v.Appointment!.Doctor!.IsDeleted && !v.Appointment.Patient!.IsDeleted);

        builder.Entity<Prescription>()
            .HasQueryFilter(p => !p.Visit!.Appointment!.Doctor!.IsDeleted && !p.Visit.Appointment.Patient!.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.CreatedByUserId = currentUserId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
                entry.Entity.UpdatedByUserId = currentUserId;

                entry.Property(e => e.CreatedAtUtc).IsModified = false;
                entry.Property(e => e.CreatedByUserId).IsModified = false;
            }
        }

        ApplySoftDelete<Doctor>(utcNow, currentUserId);
        ApplySoftDelete<Patient>(utcNow, currentUserId);
        ApplySoftDelete<Specialization>(utcNow, currentUserId);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDelete<TEntity>(DateTime utcNow, string? currentUserId)
        where TEntity : BaseEntity
    {
        foreach (var entry in ChangeTracker.Entries<TEntity>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = utcNow;
            entry.Entity.DeletedByUserId = currentUserId;
            entry.Entity.UpdatedAtUtc = utcNow;
            entry.Entity.UpdatedByUserId = currentUserId;

            entry.Property(e => e.CreatedAtUtc).IsModified = false;
            entry.Property(e => e.CreatedByUserId).IsModified = false;
        }
    }
}