using API.Models;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        User Get(string userId);
        Task Add(User user);
    }
}
