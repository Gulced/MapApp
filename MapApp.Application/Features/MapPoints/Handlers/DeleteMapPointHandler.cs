using MediatR;
using MapApp.Persistence;

public class DeleteMapPointHandler : IRequestHandler<DeleteMapPointCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public DeleteMapPointHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMapPointCommand request, CancellationToken cancellationToken)
    {
        var point = await _dbContext.MapPoints.FindAsync(request.Id);
        if (point == null) return false;

        _dbContext.MapPoints.Remove(point);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
