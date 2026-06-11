using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/seasons")]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonsController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SeasonResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateSeason([FromBody] CreateSeasonRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SeasonResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSeason(int id)
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SeasonResponse>), 200)]
    public async Task<IActionResult> GetAllSeasons()
    {
        throw new NotImplementedException();
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(SeasonResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSeasonStatus(int id, [FromBody] UpdateSeasonStatusRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id:int}/tournaments/{tournamentId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddTournamentToSeason(int id, int tournamentId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}/players/{playerId:int}/stats")]
    [ProducesResponseType(typeof(SeasonalStatsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerSeasonalStats(int id, int playerId)
    {
        throw new NotImplementedException();
    }
}
