using DrugTracker.Models;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> ValidateUserAsync(string username, string password);
        Task<Organization?> GetOrganizationByIdAsync(int orgId);
        Task UpdateUserAsync(User user);
    }
}
