using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NetTopologySuite;
using MapApp.Domain.Entities;

namespace MapApp.Persistence.Factories
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=mapappdb;Username=postgres;Password=şans",
                o => o.UseNetTopologySuite()
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
