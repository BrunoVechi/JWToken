using API.Interfaces;
using API.Models;

namespace API.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly DataBaseContext _dbContext;
        public RefreshTokenRepository(DataBaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public RefreshToken Get(string token)
        {
            return _dbContext.RefreshTokens!.FirstOrDefault(t => t.Token == token)!;
        }
        public async Task Add(RefreshToken token)
        {
            _dbContext.RefreshTokens!.Add(token);
            await _dbContext.SaveChangesAsync();
        }
        public async Task Update(RefreshToken token)
        {
            _dbContext.RefreshTokens!.Update(token);
            await _dbContext.SaveChangesAsync();
        }
    }
}
