using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    /// <summary>List all champion classes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClassResponse>), 200)]
    public async Task<IActionResult> GetAllClasses()
        => Ok(await _classService.GetAllClassesAsync());

    /// <summary>Get a single class by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClassResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClass(int id)
    {
        try { return Ok(await _classService.GetClassAsync(id)); }
        catch (ClassNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>List the skills of a class (ATTACK, DEFEND, HEAL, AURA).</summary>
    [HttpGet("{id:int}/skills")]
    [ProducesResponseType(typeof(IEnumerable<SkillResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClassSkills(int id)
    {
        try { return Ok(await _classService.GetClassSkillsAsync(id)); }
        catch (ClassNotFoundException ex) { return NotFound(ex.Message); }
    }
}
