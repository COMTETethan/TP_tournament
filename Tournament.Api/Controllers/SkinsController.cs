using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class SkinsController : ControllerBase
{
    private readonly ISkinService _skinService;

    public SkinsController(ISkinService skinService)
    {
        _skinService = skinService;
    }

    /// <summary>Create a new skin in the catalog.</summary>
    [HttpPost("api/skins")]
    [ProducesResponseType(typeof(SkinResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateSkin([FromBody] CreateSkinRequest request)
    {
        try
        {
            var skin = await _skinService.CreateSkinAsync(request);
            return CreatedAtAction(nameof(GetSkin), new { id = skin.Id }, skin);
        }
        catch (InvalidSkinCategoryException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>List all active skins.</summary>
    [HttpGet("api/skins")]
    [ProducesResponseType(typeof(IEnumerable<SkinResponse>), 200)]
    public async Task<IActionResult> GetAllSkins()
    {
        var skins = await _skinService.GetAllSkinsAsync();
        return Ok(skins);
    }

    /// <summary>Get a skin by id.</summary>
    [HttpGet("api/skins/{id:int}")]
    [ProducesResponseType(typeof(SkinResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSkin(int id)
    {
        try
        {
            var skin = await _skinService.GetSkinAsync(id);
            return Ok(skin);
        }
        catch (SkinNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Soft-delete a skin (sets is_active = false).</summary>
    [HttpDelete("api/skins/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeactivateSkin(int id)
    {
        try
        {
            await _skinService.DeactivateSkinAsync(id);
            return NoContent();
        }
        catch (SkinNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Equip a skin for a player.</summary>
    [HttpPost("api/players/{id:int}/skin")]
    [ProducesResponseType(typeof(PlayerLoadoutResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EquipPlayerSkin(int id, [FromBody] EquipPlayerSkinRequest request)
    {
        try
        {
            var loadout = await _skinService.EquipPlayerSkinAsync(id, request);
            return Ok(loadout);
        }
        catch (PlayerNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (SkinNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidSkinCategoryException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Get the current skin loadout of a player.</summary>
    [HttpGet("api/players/{id:int}/skin")]
    [ProducesResponseType(typeof(PlayerLoadoutResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerLoadout(int id)
    {
        try
        {
            var loadout = await _skinService.GetPlayerLoadoutAsync(id);
            return Ok(loadout);
        }
        catch (PlayerNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Set the background skin for a tournament.</summary>
    [HttpPost("api/tournaments/{id:int}/background")]
    [ProducesResponseType(typeof(TournamentBackgroundResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetTournamentBackground(int id, [FromBody] SetTournamentBackgroundRequest request)
    {
        try
        {
            var bg = await _skinService.SetTournamentBackgroundAsync(id, request);
            return Ok(bg);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (SkinNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidSkinCategoryException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Get the current background skin of a tournament.</summary>
    [HttpGet("api/tournaments/{id:int}/background")]
    [ProducesResponseType(typeof(TournamentBackgroundResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentBackground(int id)
    {
        try
        {
            var bg = await _skinService.GetTournamentBackgroundAsync(id);
            return Ok(bg);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
