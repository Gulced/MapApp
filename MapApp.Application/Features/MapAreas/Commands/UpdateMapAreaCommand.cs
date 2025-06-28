using MediatR;
using NetTopologySuite.Geometries;

namespace MapApp.Application.Features.MapAreas.Commands
{
    public class UpdateMapAreaCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Note { get; set; }
        public Polygon Area { get; set; } = null!;
    }
}
