using MediatR;
using MapApp.Domain.Entities;
using MapApp.Persistence;
using NetTopologySuite.Geometries;
using MapApp.Application.Features.MapPoints.Commands;

namespace MapApp.Application.Features.MapPoints.Handlers
{
    public class CreateMapPointHandler : IRequestHandler<CreateMapPointCommand, int>
    {
        private readonly AppDbContext _db;

        public CreateMapPointHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> Handle(CreateMapPointCommand request, CancellationToken cancellationToken)
        {
            var point = new MapPoint
            {
                Title = request.Title,
                Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 }
            };

            _db.MapPoints.Add(point);
            await _db.SaveChangesAsync(cancellationToken);

            return point.Id;
        }
    }
}
