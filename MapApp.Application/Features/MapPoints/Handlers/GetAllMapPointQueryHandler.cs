using MediatR;
using MapApp.Application.Features.MapPoints.Dtos;
using MapApp.Application.Features.MapPoints.Queries;
using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MapApp.Application.Features.MapPoints.Handlers
{
    public class GetAllMapPointsQueryHandler : IRequestHandler<GetAllMapPointsQuery, List<MapPointDto>>
    {
        private readonly AppDbContext _dbContext;

        public GetAllMapPointsQueryHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MapPointDto>> Handle(GetAllMapPointsQuery request, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.MapPoints.ToListAsync(cancellationToken);

            return entities.Select(point => new MapPointDto
            {
                Id = point.Id,
                Title = point.Title,
                Description = point.Description,
                Latitude = double.IsNaN(point.Location.Y) || double.IsInfinity(point.Location.Y) ? 0 : point.Location.Y,
                Longitude = double.IsNaN(point.Location.X) || double.IsInfinity(point.Location.X) ? 0 : point.Location.X
            }).ToList();
        }
    }
}
