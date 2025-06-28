using MediatR;
using MapApp.Persistence;
using System.Threading;
using System.Threading.Tasks;
using MapApp.Application.Features.MapAreas.Commands;

namespace MapApp.Application.Features.MapAreas.Handlers
{
    public class DeleteMapAreaCommandHandler : IRequestHandler<DeleteMapAreaCommand, bool>
    {
        private readonly AppDbContext _dbContext;

        public DeleteMapAreaCommandHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(DeleteMapAreaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.MapAreas.FindAsync(new object[] { request.Id }, cancellationToken);
            if (entity == null)
                return false;

            _dbContext.MapAreas.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
