using MediatR;
using MapApp.Domain.Entities;
using MapApp.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using MapApp.Application.Features.MapAreas.Commands;
namespace MapApp.Application.Features.MapAreas.Handlers
{
    public class UpdateMapAreaCommandHandler : IRequestHandler<UpdateMapAreaCommand, bool>
    {
        private readonly AppDbContext _dbContext;

        public UpdateMapAreaCommandHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(UpdateMapAreaCommand request, CancellationToken cancellationToken)
        {
            var area = await _dbContext.MapAreas.FindAsync(new object[] { request.Id }, cancellationToken);
            if (area == null) return false;

            area.Name = request.Name;
            area.Area = request.Area;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
