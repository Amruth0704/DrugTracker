using DrugTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DrugTracker.Data
{
    public class DrugTrackerDbContext : DbContext
    {
        public DrugTrackerDbContext(DbContextOptions<DrugTrackerDbContext> options) : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Drug> Drugs { get; set; }
        public DbSet<DrugBatch> DrugBatches { get; set; }
        public DbSet<BatchOwnershipHistory> BatchOwnershipHistories { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<BlockchainLedger> BlockchainLedgers { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("HealthCare");

            // Organization
            modelBuilder.Entity<Organization>()
                .Property(o => o.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");
            
            modelBuilder.Entity<Organization>()
                .Property(o => o.IsActive)
                .HasDefaultValue(true);

            // User
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);
            
            // OrgId is unique per user? No, but UserName is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            // Drug
            modelBuilder.Entity<Drug>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");
            
            modelBuilder.Entity<Drug>()
                .HasIndex(d => d.DrugCode)
                .IsUnique();

            modelBuilder.Entity<DrugBatch>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            modelBuilder.Entity<DrugBatch>()
                .HasOne(d => d.CreatedByOrg)
                .WithMany()
                .HasForeignKey(d => d.CreatedByOrgId)
                .OnDelete(DeleteBehavior.Restrict);

            // BatchOwnershipHistory
            modelBuilder.Entity<BatchOwnershipHistory>()
                .Property(b => b.ActionTime)
                .HasDefaultValueSql("SYSDATETIME()");

            modelBuilder.Entity<BatchOwnershipHistory>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BatchOwnershipHistory>()
                .HasOne(b => b.ToOrg)
                .WithMany()
                .HasForeignKey(b => b.ToOrgId)
                .OnDelete(DeleteBehavior.Restrict);

            // Inventory
            modelBuilder.Entity<Inventory>()
                .Property(i => i.LastUpdated)
                .HasDefaultValueSql("SYSDATETIME()");

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.PharmacyOrg)
                .WithMany()
                .HasForeignKey(i => i.PharmacyOrgId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // BlockchainLedger
            modelBuilder.Entity<BlockchainLedger>()
                .HasAnnotation("SqlServer:IsLedger", true)
                .HasAnnotation("SqlServer:IsAppendOnly", true)
                .Property(b => b.ActionTime)
                .HasDefaultValueSql("SYSDATETIME()");
            
            // To support "LEDGER = ON" we might need raw SQL in migration, 
            // but for EF Core model, we can map it as a normal table or temporal table.
            
            // ActivityLog
            modelBuilder.Entity<ActivityLog>()
                .Property(a => a.ActionTime)
                .HasDefaultValueSql("SYSDATETIME()");
        }
    }
}
