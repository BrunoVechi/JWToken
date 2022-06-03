using API.Models;

namespace API.Interfaces
{
    public interface IRefreshTokenRepository
    {
        RefreshToken Get(string token);
        Task Update(RefreshToken token);
        Task Add(RefreshToken token);
    }
}
