using MediatR;
using NetTopologySuite.Geometries;

namespace MapApp.Application.Features.MapAreas.Commands
{
    public class CreateMapAreaCommand : IRequest<int>
    {
        public string Name { get; set; } = default!;
        public string? Note { get; set; }
        public Polygon Area { get; set; } = default!;
    }
}
