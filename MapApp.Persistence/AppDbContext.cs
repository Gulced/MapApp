using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using MapApp.Domain.Entities;

namespace MapApp.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<MapPoint> MapPoints { get; set; }
        public DbSet<MapArea> MapAreas { get; set; }

        // Kullanıcılar için DbSet
        public DbSet<AppUser> Users { get; set; }
    }
}
