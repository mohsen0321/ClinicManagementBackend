using Microsoft.EntityFrameworkCore;
using clinicsystembackend.Models;

namespace clinicsystembackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorWorkingHour> DoctorWorkingHours { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.TimeSlot });

            modelBuilder.Entity<Specialty>().HasData(
                new Specialty { Id = 1, Name = "القلب والأوعية الدموية", Description = "تشخيص وعلاج أمراض القلب والأوعية الدموية" },
                new Specialty { Id = 2, Name = "طب الأسنان", Description = "علاج وتجميل الأسنان وجراحة الفم" },
                new Specialty { Id = 3, Name = "طب الأطفال", Description = "رعاية وصحة الأطفال منذ الولادة" },
                new Specialty { Id = 4, Name = "الجلدية والتناسلية", Description = "علاج الأمراض الجلدية والعناية بالبشرة" }
            );

            modelBuilder.Entity<Clinic>().HasData(
                new Clinic
                {
                    Id = 1,
                    Name = "عيادة النخبة الطبية المركزية",
                    Description = "عيادة تخصصية متكاملة تقدم أفضل رعاية صحية",
                    Address = "شارع التحرير، وسط البلد، القاهرة",
                    PhoneNumber = "01012345678",
                    IsActive = true
                }
            );

          
            string defaultAdminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@clinic.com",
                    PhoneNumber = "01000000000",
                    PasswordHash = defaultAdminPasswordHash,
                    Role = "Admin",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true
                }
            );
        }
    }
}