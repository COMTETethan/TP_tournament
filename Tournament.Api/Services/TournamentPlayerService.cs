using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>
/// Registrations: a champion's participation in a tournament, with the per-tournament state
/// (disqualification, penalty points). The same champion can be registered in several tournaments
/// independently.
/// </summary>
public class TournamentPlayerService : ITournamentPlayerService
{
    private readonly IPlayerService _players;
    private readonly List<RegistrationEntity> _registrations;

    public TournamentPlayerService() : this(new PlayerService()) { }

    public TournamentPlayerService(IPlayerService players)
    {
        _players = players;
        // Seed: the two example champions are entered in tournament 1 (one disqualified).
        _registrations = new List<RegistrationEntity>
        {
            new() { TournamentId = 1, PlayerId = 1, IsDisqualified = false, PenaltyPoints = 0 },
            new() { TournamentId = 1, PlayerId = 2, IsDisqualified = true,  PenaltyPoints = 0 }
        };
    }

    public async Task<RegistrationResponse> RegisterAsync(int tournamentId, int playerId)
    {
        if (!TournamentService.Exists(tournamentId))
            throw new TournamentNotFoundException(tournamentId);

        var player = await _players.GetPlayerAsync(playerId); // throws PlayerNotFoundException

        if (_registrations.Any(r => r.TournamentId == tournamentId && r.PlayerId == playerId))
            throw new PlayerAlreadyRegisteredException(tournamentId, playerId);

        var entity = new RegistrationEntity { TournamentId = tournamentId, PlayerId = playerId };
        _registrations.Add(entity);
        return Map(entity, player);
    }

    public async Task<IEnumerable<RegistrationResponse>> GetTournamentPlayersAsync(int tournamentId)
    {
        if (!TournamentService.Exists(tournamentId))
            throw new TournamentNotFoundException(tournamentId);

        var result = new List<RegistrationResponse>();
        foreach (var reg in _registrations.Where(r => r.TournamentId == tournamentId))
            result.Add(Map(reg, await _players.GetPlayerAsync(reg.PlayerId)));
        return result;
    }

    public async Task<RegistrationResponse> GetRegistrationAsync(int tournamentId, int playerId)
    {
        var reg = Find(tournamentId, playerId);
        return Map(reg, await _players.GetPlayerAsync(playerId));
    }

    public async Task<RegistrationResponse> DisqualifyAsync(int tournamentId, int playerId)
    {
        var reg = Find(tournamentId, playerId);
        reg.IsDisqualified = true;
        reg.PenaltyPoints = 0;
        return Map(reg, await _players.GetPlayerAsync(playerId));
    }

    public async Task<RegistrationResponse> AddPenaltyAsync(int tournamentId, int playerId, AddPenaltyRequest request)
    {
        if (request.PenaltyPoints < 0)
            throw new ArgumentException("PenaltyPoints must be non-negative.", nameof(request.PenaltyPoints));

        var reg = Find(tournamentId, playerId);
        reg.PenaltyPoints += request.PenaltyPoints;
        return Map(reg, await _players.GetPlayerAsync(playerId));
    }

    private RegistrationEntity Find(int tournamentId, int playerId)
        => _registrations.FirstOrDefault(r => r.TournamentId == tournamentId && r.PlayerId == playerId)
           ?? throw new RegistrationNotFoundException(tournamentId, playerId);

    private static RegistrationResponse Map(RegistrationEntity r, PlayerResponse player)
        => new(r.TournamentId, r.PlayerId, player.Name, player.ClassId, player.Level, r.IsDisqualified, r.PenaltyPoints);

    private class RegistrationEntity
    {
        public int TournamentId { get; set; }
        public int PlayerId { get; set; }
        public bool IsDisqualified { get; set; }
        public int PenaltyPoints { get; set; }
    }
}
