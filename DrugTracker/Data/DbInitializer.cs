using DrugTracker.Models;
using System;
using System.Linq;
using System.Text;

namespace DrugTracker.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DrugTrackerDbContext context)
        {
            context.Database.EnsureCreated();

            // Clear existing data (Order matters due to FKs)
            // 1. Child tables first
            context.BlockchainLedgers.RemoveRange(context.BlockchainLedgers);
            context.BatchOwnershipHistories.RemoveRange(context.BatchOwnershipHistories);
            context.Inventories.RemoveRange(context.Inventories);
            context.DrugBatches.RemoveRange(context.DrugBatches);
            
            // 2. Parent tables (except Org type lookups if any, but here Users depend on Orgs)
            context.Users.RemoveRange(context.Users);
            context.Drugs.RemoveRange(context.Drugs);
            context.Organizations.RemoveRange(context.Organizations);
            
            context.SaveChanges();

            // Look for any users.
            if (context.Users.Any())
            {
                return;   // Should be empty now
            }

            // --- Organizations ---
            var manufOrg = new Organization { OrgName = "PharmaCorp", OrgType = "MANUFACTURER", IsActive = true };
            var distOrg = new Organization { OrgName = "FastLogistics", OrgType = "DISTRIBUTOR", IsActive = true };
            var pharmOrg = new Organization { OrgName = "HealthPlus", OrgType = "PHARMACY", IsActive = true };

            context.Organizations.AddRange(manufOrg, distOrg, pharmOrg);
            context.SaveChanges();

            // --- Users ---
            // Simple password hashing simulation (matching UserRepository logic)
            var passwordHash = Encoding.UTF8.GetBytes("password");

            var users = new User[]
            {
                new User { UserName = "manuf", PasswordHash = passwordHash, Role = "Manufacturer", OrgId = manufOrg.OrgId, IsActive = true },
                new User { UserName = "dist", PasswordHash = passwordHash, Role = "Distributor", OrgId = distOrg.OrgId, IsActive = true },
                new User { UserName = "pharm", PasswordHash = passwordHash, Role = "Pharmacy", OrgId = pharmOrg.OrgId, IsActive = true },
                // Admin if needed
                new User { UserName = "admin", PasswordHash = passwordHash, Role = "Admin", OrgId = manufOrg.OrgId, IsActive = true } 
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // --- Drugs ---
            var drugs = new Drug[]
            {
                new Drug { DrugName = "Paracetamol", DrugCode = "PAR" },
                new Drug { DrugName = "Amoxicillin", DrugCode = "AMX" },
                new Drug { DrugName = "Ibuprofen", DrugCode = "IBU" }
            };
            context.Drugs.AddRange(drugs);
            context.SaveChanges();
        }
    }
}
