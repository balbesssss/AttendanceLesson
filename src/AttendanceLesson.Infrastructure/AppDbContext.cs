using AttendanceLesson.Domain.Models;
using Microsoft.EntityFrameworkCore;
using AttendanceLesson.Infrastructure.Repositories;

namespace AttendanceLesson.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Logbook> Logbooks => Set<Logbook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Group>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            e.Property(x => x.RefreshToken)
                .HasMaxLength(500);
            e.Property(x => x.RefreshTokenExpires); 

            e.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            e.HasIndex(x => x.RoleId);
            e.HasIndex(x => x.GroupId);
        });

        modelBuilder.Entity<Logbook>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(x => x.IsPresent)
                .IsRequired();

            e.Property(x => x.AbsenceDuration);

            e.Property(x => x.AbsenceReason)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired(false);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.LessonId);
            e.HasIndex(x => x.CreatedAt);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Logbook_AbsenceReason",
                "\"IsPresent\" = TRUE OR \"AbsenceReason\" IS NOT NULL"));
        });
    }
}