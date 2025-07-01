using MediatR;
using MapApp.Application.Features.MapPoints.Dtos;
using MapApp.Application.Features.MapPoints.Queries;
using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapApp.Application.Features.MapPoints.Handlers
{
    public class GetAllMapPointsQueryHandler : IRequestHandler<GetAllMapPointsQuery, List<MapPointDto>>
    {
        private readonly AppDbContext _dbContext;
        private readonly MathTransform _transformToWgs84;

        public GetAllMapPointsQueryHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;

            var csFactory = new CoordinateSystemFactory();
            var wgs84 = GeographicCoordinateSystem.WGS84;
            var webMercator = ProjectedCoordinateSystem.WebMercator;

            var transformFactory = new CoordinateTransformationFactory();
            var transform = transformFactory.CreateFromCoordinateSystems(webMercator, wgs84);
            _transformToWgs84 = transform.MathTransform;
        }

        public async Task<List<MapPointDto>> Handle(GetAllMapPointsQuery request, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.MapPoints.ToListAsync(cancellationToken);

            return entities.Select(point =>
            {
                // Koordinatları dönüştür
                var coord = _transformToWgs84.Transform(new[] { point.Location.X, point.Location.Y });
                double lon = coord[0];
                double lat = coord[1];

                return new MapPointDto
                {
                    Id = point.Id,
                    Title = point.Title,
                    Description = point.Description,
                    Longitude = lon,
                    Latitude = lat
                };
            }).ToList();
        }
    }
}
