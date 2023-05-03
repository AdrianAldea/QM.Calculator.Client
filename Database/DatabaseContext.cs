using Microsoft.EntityFrameworkCore;
using Models;

namespace Database
{
    public class DatabaseContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source = db.db");
        }

        public DbSet<User> User { get; set; }
    }
}
