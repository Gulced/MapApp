using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using MapApp.Domain.Entities;

namespace MapApp.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<MapPoint> MapPoints => Set<MapPoint>();
        public DbSet<MapArea> MapAreas => Set<MapArea>();

        // Kullanıcılar için DbSet
        public DbSet<AppUser> Users => Set<AppUser>();
    }
}
