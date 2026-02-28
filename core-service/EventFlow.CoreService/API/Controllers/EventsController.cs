using System.Security.Claims;
using EventFlow.CoreService.Application.DTOs;
using EventFlow.CoreService.Application.Interfaces;
using EventFlow.SharedKernel.DTOs;
using EventFlow.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.CoreService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _service;

    public EventsController(IEventService service) => _service = service;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User identity not found."));

    /// <summary>Get all public events (paginated).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(result));
    }

    /// <summary>Get a single event by ID.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var ev = await _service.GetByIdAsync(id, ct);
        if (ev is null) return NotFound(ApiResponse<EventDto>.Fail("Event not found."));
        return Ok(ApiResponse<EventDto>.Ok(ev));
    }

    /// <summary>Get events owned by the current user.</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _service.GetByOwnerAsync(CurrentUserId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(result));
    }

    /// <summary>Create a new event.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var ev = await _service.CreateAsync(request, CurrentUserId, correlationId, ct);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ApiResponse<EventDto>.Ok(ev));
    }

    /// <summary>Update an existing event.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var ev = await _service.UpdateAsync(id, request, CurrentUserId, correlationId, ct);
        return Ok(ApiResponse<EventDto>.Ok(ev));
    }

    /// <summary>Delete an event.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        await _service.DeleteAsync(id, CurrentUserId, correlationId, ct);
        return NoContent();
    }
}
