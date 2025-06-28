using NetTopologySuite.Geometries;

namespace MapApp.Domain.Entities
{
    public class MapPoint
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Point Location { get; set; } = null!;
    }
}
