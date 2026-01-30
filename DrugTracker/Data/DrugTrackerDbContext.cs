using DrugTracker.Models;
using DrugTracker.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;

namespace DrugTracker.Data
{
    public class DrugTrackerDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DrugTrackerDbContext(DbContextOptions<DrugTrackerDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Drug> Drugs { get; set; }
        public DbSet<DrugBatch> DrugBatches { get; set; }
        public DbSet<BatchOwnershipHistory> BatchOwnershipHistories { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<BlockchainLedger> BlockchainLedgers { get; set; }

        // DTOs for Stored Procedures (Keyless)
        public DbSet<ManufacturerDashboardRow> ManufacturerDashboardData { get; set; }
        public DbSet<DistributorDashboardRow> DistributorDashboardData { get; set; }
        public DbSet<PharmacyDashboardRow> PharmacyDashboardData { get; set; }
        public DbSet<PharmacyInventoryRow> PharmacyInventoryData { get; set; }

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
            
            // BlockchainLedger
            modelBuilder.Entity<BlockchainLedger>()
                .HasAnnotation("SqlServer:IsLedger", true)
                .HasAnnotation("SqlServer:IsAppendOnly", true)
                .Property(b => b.ActionTime)
                .HasDefaultValueSql("SYSDATETIME()");

            // Keyless DTOs
            modelBuilder.Entity<ManufacturerDashboardRow>().HasNoKey();
            modelBuilder.Entity<DistributorDashboardRow>().HasNoKey();
            modelBuilder.Entity<PharmacyDashboardRow>().HasNoKey();
            modelBuilder.Entity<PharmacyInventoryRow>().HasNoKey();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await SetSessionContextAsync();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            SetSessionContextAsync().GetAwaiter().GetResult();
            return base.SaveChanges();
        }

        public async Task SetSessionContextAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst("UserId")?.Value;
                var orgId = httpContext.User.FindFirst("OrgId")?.Value;
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(role))
                {
                    var connection = Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            EXEC sys.sp_set_session_context @key = N'UserId', @value = @userId;
                            EXEC sys.sp_set_session_context @key = N'OrgId', @value = @orgId;
                            EXEC sys.sp_set_session_context @key = N'Role', @value = @role;
                            EXEC sys.sp_set_session_context @key = N'IPAddress', @value = @ipAddress;";

                        var pUserId = command.CreateParameter(); pUserId.ParameterName = "@userId"; pUserId.Value = userId; command.Parameters.Add(pUserId);
                        var pOrgId = command.CreateParameter(); pOrgId.ParameterName = "@orgId"; pOrgId.Value = orgId; command.Parameters.Add(pOrgId);
                        var pRole = command.CreateParameter(); pRole.ParameterName = "@role"; pRole.Value = role; command.Parameters.Add(pRole);
                        var pIP = command.CreateParameter(); pIP.ParameterName = "@ipAddress"; pIP.Value = (object)ipAddress ?? DBNull.Value; command.Parameters.Add(pIP);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }
}
