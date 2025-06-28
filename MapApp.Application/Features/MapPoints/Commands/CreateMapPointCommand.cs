using MediatR;
using NetTopologySuite.Geometries;

namespace MapApp.Application.Features.MapPoints.Commands
{
    public class CreateMapPointCommand : IRequest<int>
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
