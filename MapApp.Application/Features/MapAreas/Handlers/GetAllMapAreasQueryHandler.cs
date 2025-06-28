using MediatR;
using MapApp.Domain.Entities;
using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapApp.Application.Features.MapAreas.Queries;

namespace MapApp.Application.Features.MapAreas.Handlers
{
    public class GetAllMapAreasQueryHandler : IRequestHandler<GetAllMapAreasQuery, List<MapArea>>
    {
        private readonly AppDbContext _dbContext;

        public GetAllMapAreasQueryHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MapArea>> Handle(GetAllMapAreasQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.MapAreas.ToListAsync(cancellationToken);
        }
    }
}
