namespace MapApp.Application.Features.MapAreas.Dtos
{
    public class MapAreaDto
    {
        public int? Id { get; set; } // create sırasında null olabilir
        public string Name { get; set; } = default!;
        public string? Note { get; set; }

        // GeoJSON formatına uygun dizi: [[[lng, lat], ...]]
        public List<List<List<double>>> Coordinates { get; set; } = default!;
    }
}
