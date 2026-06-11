using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>
/// In-memory challenges between users. Accepting a challenge creates a combat
/// (default Knight, level 5) and exposes its id so the front can play it.
/// The store is static so it persists across requests.
/// </summary>
public class ChallengeService : IChallengeService
{
    private const string Pending  = "PENDING";
    private const string Accepted = "ACCEPTED";
    private const string Declined = "DECLINED";

    private const int DefaultClassId = 1; // Knight
    private const int DefaultLevel   = 5;

    private static readonly object Lock = new();
    private static readonly List<ChallengeEntity> Store = new();
    private static int _nextId = 1;

    private readonly ICombatService _combatService;

    public ChallengeService(ICombatService combatService) => _combatService = combatService;

    public Task<ChallengeResponse> CreateChallengeAsync(CreateChallengeRequest request)
    {
        if (request.ChallengerUserId == request.OpponentUserId)
            throw new ArgumentException("You cannot challenge yourself.");
        if (!AuthService.UserExists(request.ChallengerUserId))
            throw new UserNotFoundException(request.ChallengerUserId);
        if (!AuthService.UserExists(request.OpponentUserId))
            throw new UserNotFoundException(request.OpponentUserId);

        ChallengeEntity entity;
        lock (Lock)
        {
            entity = new ChallengeEntity
            {
                Id               = _nextId++,
                ChallengerUserId = request.ChallengerUserId,
                OpponentUserId   = request.OpponentUserId,
                Status           = Pending,
                CombatId         = null,
                CreatedAt        = DateTime.UtcNow
            };
            Store.Add(entity);
        }
        return Task.FromResult(Map(entity));
    }

    public Task<IEnumerable<ChallengeResponse>> GetUserChallengesAsync(int userId)
    {
        lock (Lock)
        {
            var list = Store
                .Where(c => c.ChallengerUserId == userId || c.OpponentUserId == userId)
                .Select(Map)
                .ToList();
            return Task.FromResult<IEnumerable<ChallengeResponse>>(list);
        }
    }

    public async Task<ChallengeResponse> AcceptChallengeAsync(int challengeId)
    {
        ChallengeEntity entity;
        lock (Lock)
        {
            entity = Store.FirstOrDefault(c => c.Id == challengeId)
                     ?? throw new ChallengeNotFoundException(challengeId);
        }

        // Accepting spawns a real combat between the two users.
        var combat = await _combatService.StartCombatAsync(new CreateCombatRequest(
            new CombatantSpec(AuthService.EmailOf(entity.ChallengerUserId) ?? "Challenger", DefaultClassId, DefaultLevel),
            new CombatantSpec(AuthService.EmailOf(entity.OpponentUserId)   ?? "Opponent",   DefaultClassId, DefaultLevel)));

        lock (Lock)
        {
            entity.Status   = Accepted;
            entity.CombatId = combat.Id;
            return Map(entity);
        }
    }

    public Task<ChallengeResponse> DeclineChallengeAsync(int challengeId)
    {
        lock (Lock)
        {
            var entity = Store.FirstOrDefault(c => c.Id == challengeId)
                         ?? throw new ChallengeNotFoundException(challengeId);
            entity.Status = Declined;
            return Task.FromResult(Map(entity));
        }
    }

    private static ChallengeResponse Map(ChallengeEntity c)
        => new(
            c.Id,
            c.ChallengerUserId, AuthService.EmailOf(c.ChallengerUserId) ?? "",
            c.OpponentUserId,   AuthService.EmailOf(c.OpponentUserId)   ?? "",
            c.Status, c.CombatId, c.CreatedAt);

    private class ChallengeEntity
    {
        public int Id { get; set; }
        public int ChallengerUserId { get; set; }
        public int OpponentUserId { get; set; }
        public string Status { get; set; } = Pending;
        public int? CombatId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
