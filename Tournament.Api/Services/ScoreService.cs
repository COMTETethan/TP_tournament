using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

public class ScoreService : IScoreService
{
    private readonly IPlayerService _playerService;
    private readonly IDuelService _duelService;
    private readonly ITournamentService _tournamentService;

    public ScoreService(IPlayerService playerService, IDuelService duelService, ITournamentService tournamentService)
    {
        _playerService = playerService;
        _duelService = duelService;
        _tournamentService = tournamentService;
    }

    public async Task<PlayerScoreResponse> GetPlayerScoreAsync(int playerId)
    {
        // Verify player exists
        var player = await _playerService.GetPlayerAsync(playerId);

        if (player.IsDisqualified)
            return new PlayerScoreResponse(playerId, player.Name, 0, true);

        // Calculate score from duel outcomes
        int wins = 0;
        int losses = 0;
        int draws = 0;

        // Get all duels to find player's matches (simplified: assume tournaments share player data)
        // In real scenario, we'd query by player in a specific tournament
        // For now, iterate through duels where playerId was a participant
        var allDuels = new List<DuelResponse>();
        try
        {
            // Try to get from tournament 1 (tests use this)
            var duels = await _duelService.GetTournamentDuelsAsync(player.TournamentId);
            allDuels.AddRange(duels);
        }
        catch
        {
            // If tournament not found, no duels
        }

        foreach (var duel in allDuels)
        {
            if (duel.Outcome is null) continue;

            if (duel.Player1Id == playerId)
            {
                if (duel.Outcome == "PLAYER1_WIN") wins++;
                else if (duel.Outcome == "PLAYER2_WIN") losses++;
                else if (duel.Outcome == "DRAW") draws++;
            }
            else if (duel.Player2Id == playerId)
            {
                if (duel.Outcome == "PLAYER2_WIN") wins++;
                else if (duel.Outcome == "PLAYER1_WIN") losses++;
                else if (duel.Outcome == "DRAW") draws++;
            }
        }

        // Score calculation: 3 points per win, -1 per loss, penalty points
        int baseScore = (wins * 3) + (draws * 1) - losses;
        int penalties = player.PenaltyPoints;
        int finalScore = Math.Max(0, baseScore - penalties);

        return new PlayerScoreResponse(playerId, player.Name, finalScore, false);
    }

    public async Task<RankingResponse> GetTournamentRankingAsync(int tournamentId)
    {
        // Verify tournament exists
        await _tournamentService.GetTournamentAsync(tournamentId);

        // Get all players in tournament
        var players = await _playerService.GetTournamentPlayersAsync(tournamentId);
        var scores = new List<PlayerScoreResponse>();

        foreach (var player in players)
        {
            var score = await GetPlayerScoreAsync(player.Id);
            scores.Add(score);
        }

        // Sort by score descending
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
}
