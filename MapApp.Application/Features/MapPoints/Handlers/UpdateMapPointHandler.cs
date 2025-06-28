using MediatR;
using MapApp.Persistence;

public class UpdateMapPointHandler : IRequestHandler<UpdateMapPointCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public UpdateMapPointHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMapPointCommand request, CancellationToken cancellationToken)
    {
        var point = await _dbContext.MapPoints.FindAsync(request.Id);
        if (point == null) return false;

        point.Title = request.Title;
        point.Description = request.Description;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
