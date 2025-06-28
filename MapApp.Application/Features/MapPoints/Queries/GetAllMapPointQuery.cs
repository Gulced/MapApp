using MediatR;
using MapApp.Application.Features.MapPoints.Dtos;
using System.Collections.Generic;

namespace MapApp.Application.Features.MapPoints.Queries
{
    public class GetAllMapPointsQuery : IRequest<List<MapPointDto>>
    {
    }
}
