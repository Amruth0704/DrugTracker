using DrugTracker.Data;
using DrugTracker.Models;
using DrugTracker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly DrugTrackerDbContext _context;

        public UserRepository(DrugTrackerDbContext context)
        {
            _context = context;
        }

        /*===============================================
          READ: Organization by Id (VIEW)
        ===============================================*/
        public async Task<Organization?> GetOrganizationByIdAsync(int orgId)
        {
            return await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrgId == orgId);
        }

        /*===============================================
          READ: User by Username (VIEW)
        ===============================================*/
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Organization)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        /*===============================================
          AUTH: Validate User (SP, no composition)
        ===============================================*/
        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            var users = await _context.Users
                .FromSqlRaw(
                    @"EXEC HealthCare.sp_ValidateUser
                        @UserName,
                        @PasswordHash",
                    new SqlParameter("@UserName", username),
                    new SqlParameter("@PasswordHash", passwordBytes))
                .AsNoTracking()
                .ToListAsync(); // ⚠️ NO composition

            return users.Count > 0 ? users[0] : null;
        }

        /*===============================================
          WRITE: Update User (SP)
        ===============================================*/
        public async Task UpdateUserAsync(User user)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"EXEC HealthCare.sp_UpdateUser
                    @UserId,
                    @Role,
                    @OrgId,
                    @IsActive,
                    @AccessFailedCount,
                    @LockoutEnd",
                new SqlParameter("@UserId", user.UserId),
                new SqlParameter("@Role", user.Role),
                new SqlParameter("@OrgId", user.OrgId),
                new SqlParameter("@IsActive", user.IsActive),
                new SqlParameter("@AccessFailedCount", user.AccessFailedCount),
                new SqlParameter("@LockoutEnd", (object?)user.LockoutEnd ?? DBNull.Value)
            );
        }
    }
}
