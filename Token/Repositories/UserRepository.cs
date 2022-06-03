using API.Interfaces;
using API.Models;

namespace API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataBaseContext _dbContext;
        public UserRepository(DataBaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public User Get(string userId)
        {
            return _dbContext.Users!.FirstOrDefault(x => x.Id == Int32.Parse(userId))!;
        }
        public async Task Add(User user)
        {
            var test = _dbContext.Users!.FirstOrDefault(x => x == user);

            if (test == null)
                _dbContext.Users!.Add(user);
            else
                _dbContext.Users!.Update(user);

            await _dbContext.SaveChangesAsync();
        }
    }
}

