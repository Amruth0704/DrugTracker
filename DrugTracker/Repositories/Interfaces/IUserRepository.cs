using DrugTracker.Models;
using System.Threading.Tasks;

namespace DrugTracker.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> ValidateUserAsync(string username, string password);
        Task UpdateUserAsync(User user);
        Task<Organization?> GetOrganizationByIdAsync(int orgId);
    }
}
