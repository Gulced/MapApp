using NetTopologySuite.Geometries;

namespace MapApp.Domain.Entities
{
    public class MapArea
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Note { get; set; }
        public Polygon Area { get; set; } = null!;
    }
}
