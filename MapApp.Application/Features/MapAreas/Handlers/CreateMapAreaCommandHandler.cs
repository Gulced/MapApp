using MediatR;
using MapApp.Domain.Entities;
using MapApp.Persistence;
using System.Threading;
using System.Threading.Tasks;
using MapApp.Application.Features.MapAreas.Commands;


namespace MapApp.Application.Features.MapAreas.Handlers
{
    public class CreateMapAreaCommandHandler : IRequestHandler<CreateMapAreaCommand, int>
    {
        private readonly AppDbContext _dbContext;

        public CreateMapAreaCommandHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> Handle(CreateMapAreaCommand request, CancellationToken cancellationToken)
        {
            var mapArea = new MapArea
            {
                Name = request.Name,
                Area = request.Area
            };

            _dbContext.MapAreas.Add(mapArea);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return mapArea.Id;
        }
    }
}
