using Microsoft.EntityFrameworkCore;

namespace API.Models
{
    public class DataBaseContext : DbContext
    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options) { }

        public DbSet<User>? Users { get; set; }
        public DbSet<RefreshToken>? RefreshTokens { get; set; }

    }
}
