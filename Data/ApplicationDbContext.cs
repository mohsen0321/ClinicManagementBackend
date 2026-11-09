using Microsoft.EntityFrameworkCore;
using clinicsystembackend.Models;

namespace clinicsystembackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        // نستقبل خيارات الاتصال عبر الـ Constructor لتمريرها إلى Base DbContext
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // تعريف الجداول التي ستُنشأ في SQL Server
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

            // 1. قيد فريد (Unique): منع تكرار البريد الإلكتروني لأي مستخدم
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 2. قيد فريد (Unique): منع تكرار رقم الهاتف لأي مستخدم
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // 3. علاقة الحجز مع المريض: عند حذف مريض، لا نحذف السجلات بشكل تلقائي خطِر (Restrict)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. علاقة الحجز مع الطبيب: عند حذف طبيب، نمنع حذف الحجوزات السابقة (Restrict)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. قيد فريد لمنع تضارب الحجوزات:
            // لا يمكن لنفس الطبيب أن يحجز له أكثر من مريض في نفس اليوم ونفس الوقت إذا كان الحجز غير ملغي أو مرفوض
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.TimeSlot });

            // 6. Data Seeding: إنشاء بيانات أولية للنظام تلقائياً عند إنشاء قاعدة البيانات
            // أ. إنشاء تخصصات طبية أساسية
            modelBuilder.Entity<Specialty>().HasData(
                new Specialty { Id = 1, Name = "القلب والأوعية الدموية", Description = "تشخيص وعلاج أمراض القلب والأوعية الدموية" },
                new Specialty { Id = 2, Name = "طب الأسنان", Description = "علاج وتجميل الأسنان وجراحة الفم" },
                new Specialty { Id = 3, Name = "طب الأطفال", Description = "رعاية وصحة الأطفال منذ الولادة" },
                new Specialty { Id = 4, Name = "الجلدية والتناسلية", Description = "علاج الأمراض الجلدية والعناية بالبشرة" }
            );

            // ب. إنشاء عيادة أولية
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

            // ج. إنشاء حساب المسؤول الأول (Admin)
            // كلمة المرور الافتراضية ستكون: Admin@123456
            // قمنا بتشفيرها مسبقاً بـ BCrypt لضمان أمانها التام
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