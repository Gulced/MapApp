using MediatR;
using MapApp.Domain.Entities;

namespace MapApp.Application.Features.MapAreas.Queries
{
    public class GetMapAreaByIdQuery : IRequest<MapArea>
    {
        public int Id { get; }

        public GetMapAreaByIdQuery(int id)
        {
            Id = id;
        }
    }
}
