using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly IClassService _classService;

    public SkillsController(IClassService classService)
    {
        _classService = classService;
    }

    /// <summary>Get a single skill by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SkillResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSkill(int id)
    {
        try { return Ok(await _classService.GetSkillAsync(id)); }
        catch (SkillNotFoundException ex) { return NotFound(ex.Message); }
    }
}
