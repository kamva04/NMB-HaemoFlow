using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        // Users
        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Technician> Technicians { get; set; }

        // Medical
        public DbSet<MedicalCondition> MedicalConditions { get; set; }
        public DbSet<MedicalConditionCategory> MedicalConditionCategories { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<MedicationCategory> MedicationCategories { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<AllergyCategory> AllergyCategories { get; set; }
        public DbSet<DoctorPatientConsent> DoctorPatientConsents { get; set; }
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }

        // Laboratory
        public DbSet<TestRequest> TestRequests { get; set; }
        public DbSet<RequestTestType> RequestTestTypes { get; set; }
        public DbSet<RequestCancellation> RequestCancellations { get; set; }
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<TestTypeConsumable> TestTypeConsumables { get; set; }
        public DbSet<TechnicianTestType> TechnicianTestTypes { get; set; }
        public DbSet<RequestSample> RequestSamples { get; set; }
        public DbSet<SampleType> SampleTypes { get; set; }
        public DbSet<ResultRelease> ResultReleases { get; set; }
        public DbSet<TestResultHistory> TestResultHistories { get; set; }
        public DbSet<PatientAppointment> PatientAppointments { get; set; }
        public DbSet<TestConsumableUsage> TestConsumableUsages { get; set; }


        // Stock
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<StockOrder> StockOrders { get; set; }
        public DbSet<StockOrderItem> StockOrderItems { get; set; }
        // Consumable Management
        public DbSet<ConsumableRequest> ConsumableRequests { get; set; }
        public DbSet<StockProblemReport> StockProblemReports { get; set; }
        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PatientCondition>()
                .HasKey(pc => new { pc.PatientId, pc.ConditionId });
            modelBuilder.Entity<PatientAllergy>()
                .HasKey(pa => new { pa.PatientId, pa.AllergyId });
            modelBuilder.Entity<PatientMedication>()
                .HasKey(pm => new { pm.PatientId, pm.MedicationId });
            modelBuilder.Entity<TestTypeConsumable>()
                .HasKey(tc => new { tc.TestTypeId, tc.ConsumableId });
            modelBuilder.Entity<TechnicianTestType>()
                .HasKey(tt => new { tt.TechnicianId, tt.TestTypeId });

            // Fix decimal precision warnings
            modelBuilder.Entity<TestType>()
                .Property(t => t.NormalRangeMin).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TestType>()
                .Property(t => t.NormalRangeMax).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<RequestTestType>()
                .Property(r => r.ResultValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TestResultHistory>()
                .Property(h => h.ResultValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<RequestTestType>()
                .HasOne(rtt => rtt.Request)
                .WithMany(tr => tr.RequestTestTypes)
                .HasForeignKey(rtt => rtt.RequestId);
            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.DoctorPatientConsents)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PatientAppointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PatientAppointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PatientAppointment>()
                .HasOne(a => a.Request)
                .WithMany()
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.Doctor)
                .WithMany()
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.Request)
                .WithMany()
                .HasForeignKey(c => c.RequestId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<RequestTestType>()
                .HasOne(rtt => rtt.TestType)
                .WithMany()
                .HasForeignKey(rtt => rtt.TestTypeId);
            modelBuilder.Entity<RequestSample>()
                .HasOne(sample => sample.RequestTest)
                .WithMany()
                .HasForeignKey(sample => sample.RequestTestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockOrderItem>()
                .HasOne(i => i.Consumable)
                .WithMany()
                .HasForeignKey(i => i.ConsumableId);

            modelBuilder.Entity<StockOrder>()
                .HasOne(o => o.Supplier)
                .WithMany()
                .HasForeignKey(o => o.SupplierId);

            modelBuilder.Entity<Consumable>()
                .HasOne(c => c.Supplier)
                .WithMany()
                .HasForeignKey(c => c.SupplierId);


            modelBuilder.Entity<DoctorPatientConsent>()
            .HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.Doctor)
                .WithMany()
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DoctorPatientConsent>()
            .HasIndex(c => new
            {
                c.PatientId,
                c.DoctorId,
                c.RequestId,
                c.RequestTestId,
                c.SampleId
            })
            .IsUnique();

            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.RequestTest)
                .WithMany()
                .HasForeignKey(c => c.RequestTestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorPatientConsent>()
                .HasOne(c => c.Sample)
                .WithMany()
                .HasForeignKey(c => c.SampleId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TestRequest>()
        .HasOne(tr => tr.Patient)
        .WithMany(p => p.TestRequests)
        .HasForeignKey(tr => tr.PatientId)
        .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TestRequest>()
                .HasOne(tr => tr.Doctor)
                .WithMany(d => d.TestRequests)
                .HasForeignKey(tr => tr.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            // Notification → User (no cascade so deleting a user doesn't wipe notifications silently)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Index for fast lookups by user
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });
        }

    }

}
