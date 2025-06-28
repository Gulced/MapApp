using MapApp.Application.Features.MapAreas.Commands;
using MapApp.Application.Features.MapAreas.Queries;
using MapApp.Application.Features.MapAreas.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace MapApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapAreaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MapAreaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // 🔸 Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MapAreaDto dto)
        {
            var polygon = ConvertToPolygon(dto);

            var command = new CreateMapAreaCommand
            {
                Name = dto.Name,
                Note = dto.Note,
                Area = polygon
            };

            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        // 🔸 Get All
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllMapAreasQuery());
            return Ok(result);
        }

        // 🔸 Get By Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetMapAreaByIdQuery(id));
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // 🔸 Update
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MapAreaDto dto)
        {
            if (dto.Id != null && dto.Id != id)
                return BadRequest("ID mismatch.");

            var polygon = ConvertToPolygon(dto);

            var command = new UpdateMapAreaCommand
            {
                Id = id,
                Name = dto.Name,
                Note = dto.Note,
                Area = polygon
            };

            var updated = await _mediator.Send(command);
            return updated ? Ok(new { Message = "Updated successfully" }) : NotFound();
        }

        // 🔸 Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _mediator.Send(new DeleteMapAreaCommand(id));
            return deleted ? Ok(new { Message = "Deleted successfully" }) : NotFound();
        }

        // 🔁 Yardımcı metod: GeoJSON'dan Polygon üret
        private Polygon ConvertToPolygon(MapAreaDto dto)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var coordinates = dto.Coordinates[0]
                .Select(c => new Coordinate(c[0], c[1]))
                .ToArray();
            var linearRing = geometryFactory.CreateLinearRing(coordinates);
            return geometryFactory.CreatePolygon(linearRing);
        }
    }
}
