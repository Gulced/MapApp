using MediatR;

namespace MapApp.Application.Features.MapAreas.Commands
{
    public class DeleteMapAreaCommand : IRequest<bool>
    {
        public int Id { get; }

        public DeleteMapAreaCommand(int id)
        {
            Id = id;
        }
    }
}
