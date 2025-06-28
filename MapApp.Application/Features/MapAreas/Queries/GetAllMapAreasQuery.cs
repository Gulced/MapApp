using MediatR;
using MapApp.Domain.Entities;
using System.Collections.Generic;

namespace MapApp.Application.Features.MapAreas.Queries
{
    public class GetAllMapAreasQuery : IRequest<List<MapArea>>
    {
    }
}

