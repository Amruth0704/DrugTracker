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

            // Check if database is already seeded
            if (context.Organizations.Any())
            {
                return;   // DB has been seeded
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
