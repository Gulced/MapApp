using MediatR;
using MapApp.Domain.Entities;
using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using MapApp.Application.Features.MapAreas.Queries;

namespace MapApp.Application.Features.MapAreas.Handlers
{
    public class GetMapAreaByIdQueryHandler : IRequestHandler<GetMapAreaByIdQuery, MapArea>
    {
        private readonly AppDbContext _dbContext;

        public GetMapAreaByIdQueryHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MapArea> Handle(GetMapAreaByIdQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.MapAreas.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        }
    }
}
