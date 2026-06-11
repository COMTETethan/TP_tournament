using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

/// <summary>
/// Computes tournament scores. A champion is scored within a tournament from that tournament's
/// duels and its registration state (3 pts/win, +1/draw, −1/loss, minus penalties, floored at 0;
/// a disqualified registration scores 0).
/// </summary>
public class ScoreService : IScoreService
{
    private readonly ITournamentPlayerService _registrations;
    private readonly IDuelService _duelService;
    private readonly ITournamentService _tournamentService;

    public ScoreService()
        : this(new TournamentPlayerService(), new DuelService(), new TournamentService()) { }

    public ScoreService(ITournamentPlayerService registrations, IDuelService duelService, ITournamentService tournamentService)
    {
        _registrations = registrations;
        _duelService = duelService;
        _tournamentService = tournamentService;
    }

    public async Task<PlayerScoreResponse> GetPlayerScoreAsync(int tournamentId, int playerId)
    {
        // Throws RegistrationNotFoundException if the champion is not in this tournament.
        var registration = await _registrations.GetRegistrationAsync(tournamentId, playerId);
        return await ScoreForAsync(tournamentId, registration);
    }

    public async Task<RankingResponse> GetTournamentRankingAsync(int tournamentId)
    {
        await _tournamentService.GetTournamentAsync(tournamentId); // throws TournamentNotFoundException

        var registrations = await _registrations.GetTournamentPlayersAsync(tournamentId);

        var scores = new List<PlayerScoreResponse>();
        foreach (var registration in registrations)
            scores.Add(await ScoreForAsync(tournamentId, registration));

        var sorted = scores.OrderByDescending(s => s.FinalScore).ToList();
        return new RankingResponse(tournamentId, sorted.AsReadOnly());
    }

    public async Task<PlayerScoreResponse> GetTournamentChampionAsync(int tournamentId)
    {
        var ranking = await GetTournamentRankingAsync(tournamentId);
        if (ranking.Ranking.Count == 0)
            throw new InvalidOperationException("No players in tournament.");
        return ranking.Ranking.First();
    }

    private async Task<PlayerScoreResponse> ScoreForAsync(int tournamentId, RegistrationResponse registration)
    {
        if (registration.IsDisqualified)
            return new PlayerScoreResponse(registration.PlayerId, registration.PlayerName, 0, true);

        var duels = await _duelService.GetTournamentDuelsAsync(tournamentId);

        int wins = 0, losses = 0, draws = 0;
        foreach (var duel in duels)
        {
            if (duel.Outcome is null) continue;

            if (duel.Player1Id == registration.PlayerId)
            {
                if (duel.Outcome == "PLAYER1_WIN") wins++;
                else if (duel.Outcome == "PLAYER2_WIN") losses++;
                else if (duel.Outcome == "DRAW") draws++;
            }
            else if (duel.Player2Id == registration.PlayerId)
            {
                if (duel.Outcome == "PLAYER2_WIN") wins++;
                else if (duel.Outcome == "PLAYER1_WIN") losses++;
                else if (duel.Outcome == "DRAW") draws++;
            }
        }

        int baseScore = (wins * 3) + (draws * 1) - losses;
        int finalScore = Math.Max(0, baseScore - registration.PenaltyPoints);
        return new PlayerScoreResponse(registration.PlayerId, registration.PlayerName, finalScore, false);
    }
}
