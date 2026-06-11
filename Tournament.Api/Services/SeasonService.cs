using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class SeasonService : ISeasonService
{
    private readonly List<SeasonEntity> _seasons = new();
    private readonly List<(int SeasonId, int TournamentId)> _tournamentSeasons = new();
    private readonly List<SeasonalStatsEntity> _stats = new();
    private int _nextId = 1;

    public Task<SeasonResponse> CreateSeasonAsync(CreateSeasonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));
        if (request.EndDate <= request.StartDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(request.EndDate));

        var entity = new SeasonEntity
        {
            Id = _nextId++,
            Name = request.Name,
            Status = "UPCOMING",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };
        _seasons.Add(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<SeasonResponse> GetSeasonAsync(int id)
    {
        var entity = _seasons.FirstOrDefault(s => s.Id == id)
            ?? throw new SeasonNotFoundException(id);
        return Task.FromResult(Map(entity));
    }

    public Task<IEnumerable<SeasonResponse>> GetAllSeasonsAsync()
        => Task.FromResult<IEnumerable<SeasonResponse>>(_seasons.Select(Map).ToList());

    public Task<SeasonResponse> UpdateSeasonStatusAsync(int id, UpdateSeasonStatusRequest request)
    {
        var entity = _seasons.FirstOrDefault(s => s.Id == id)
            ?? throw new SeasonNotFoundException(id);

        var valid = (entity.Status, request.Status) switch
        {
            ("UPCOMING", "ACTIVE") => true,
            ("ACTIVE",   "ENDED")  => true,
            _ => false
        };

        if (!valid)
            throw new InvalidSeasonStatusException(request.Status);

        entity.Status = request.Status;
        return Task.FromResult(Map(entity));
    }

    public Task AddTournamentToSeasonAsync(int seasonId, int tournamentId)
    {
        if (!_seasons.Any(s => s.Id == seasonId))
            throw new SeasonNotFoundException(seasonId);

        if (!_tournamentSeasons.Contains((seasonId, tournamentId)))
            _tournamentSeasons.Add((seasonId, tournamentId));

        return Task.CompletedTask;
    }

    public Task<SeasonalStatsResponse> GetPlayerSeasonalStatsAsync(int seasonId, int playerId)
    {
        if (!_seasons.Any(s => s.Id == seasonId))
            throw new SeasonNotFoundException(seasonId);

        var stats = _stats.FirstOrDefault(s => s.SeasonId == seasonId && s.PlayerId == playerId);
        if (stats is null)
        {
            stats = new SeasonalStatsEntity { SeasonId = seasonId, PlayerId = playerId };
            _stats.Add(stats);
        }
        return Task.FromResult(MapStats(stats));
    }

    private static SeasonResponse Map(SeasonEntity e)
        => new(e.Id, e.Name, e.Status, e.StartDate, e.EndDate, e.CreatedAt);

    private static SeasonalStatsResponse MapStats(SeasonalStatsEntity e)
        => new(e.PlayerId, e.SeasonId, e.TotalScore, e.TournamentsPlayed, e.TotalWins, e.TotalLosses, e.TotalDraws, e.WinStreakBest, e.SeasonRank);

    private class SeasonEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "UPCOMING";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class SeasonalStatsEntity
    {
        public int PlayerId { get; set; }
        public int SeasonId { get; set; }
        public int TotalScore { get; set; }
        public int TournamentsPlayed { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalDraws { get; set; }
        public int WinStreakBest { get; set; }
        public int? SeasonRank { get; set; }
    }
}
