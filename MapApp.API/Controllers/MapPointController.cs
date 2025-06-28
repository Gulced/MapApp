using MediatR;
using Microsoft.AspNetCore.Mvc;
using MapApp.Application.Features.MapPoints.Commands;
using MapApp.Application.Features.MapPoints.Queries;
using MapApp.Domain.Entities;

namespace MapApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapPointController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MapPointController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/MapPoint
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMapPointCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        // GET: api/MapPoint
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllMapPointsQuery());
            return Ok(result);
        }

        // PUT: api/MapPoint/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMapPointCommand command)
        {
            if (id != command.Id) return BadRequest();
            var result = await _mediator.Send(command);
            return result ? Ok() : NotFound();
        }

        // DELETE: api/MapPoint/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteMapPointCommand { Id = id });
            return result ? Ok() : NotFound();
        }
    }
}
