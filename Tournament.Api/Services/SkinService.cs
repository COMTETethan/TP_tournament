using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class SkinService : ISkinService
{
    private static readonly object Lock = new();
    private static readonly List<SkinEntity> SkinStore = new();
    private static readonly List<PlayerLoadoutEntity> LoadoutStore = new();
    private static readonly List<TournamentBackgroundEntity> BackgroundStore = new();
    private static int NextSkinId = 1;

    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLAYER", "BACKGROUND"
    };

    // Default skins matching the SQL seed in 02_phase2.sql
    static SkinService()
    {
        SkinStore.Add(new SkinEntity { Id = 1, Category = "PLAYER",     Name = "Classic Knight", AssetKey = "skins/player/classic",       IsPremium = false, IsActive = true, CreatedAt = DateTime.UtcNow });
        SkinStore.Add(new SkinEntity { Id = 2, Category = "PLAYER",     Name = "Golden Paladin", AssetKey = "skins/player/golden_paladin", IsPremium = true,  IsActive = true, CreatedAt = DateTime.UtcNow });
        SkinStore.Add(new SkinEntity { Id = 3, Category = "BACKGROUND", Name = "Stone Arena",    AssetKey = "skins/bg/stone_arena",        IsPremium = false, IsActive = true, CreatedAt = DateTime.UtcNow });
        SkinStore.Add(new SkinEntity { Id = 4, Category = "BACKGROUND", Name = "Ghost Castle",   AssetKey = "skins/bg/ghost_castle",       IsPremium = true,  IsActive = true, CreatedAt = DateTime.UtcNow });
        NextSkinId = 5;
    }

    public Task<SkinResponse> CreateSkinAsync(CreateSkinRequest request)
    {
        if (!ValidCategories.Contains(request.Category))
            throw new InvalidSkinCategoryException(request.Category);

        lock (Lock)
        {
            var entity = new SkinEntity
            {
                Id = NextSkinId++,
                Category = request.Category.ToUpperInvariant(),
                Name = request.Name,
                AssetKey = request.AssetKey,
                IsPremium = request.IsPremium,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            SkinStore.Add(entity);
            return Task.FromResult(Map(entity));
        }
    }

    public Task<IEnumerable<SkinResponse>> GetAllSkinsAsync()
    {
        lock (Lock)
        {
            var active = SkinStore.Where(s => s.IsActive).Select(Map).ToList();
            return Task.FromResult<IEnumerable<SkinResponse>>(active);
        }
    }

    public Task<SkinResponse> GetSkinAsync(int id)
    {
        lock (Lock)
        {
            var skin = FindOrThrow(id);
            return Task.FromResult(Map(skin));
        }
    }

    public Task DeactivateSkinAsync(int id)
    {
        lock (Lock)
        {
            FindOrThrow(id).IsActive = false;
        }
        return Task.CompletedTask;
    }

    public Task<PlayerLoadoutResponse> EquipPlayerSkinAsync(int playerId, EquipPlayerSkinRequest request)
    {
        lock (Lock)
        {
            var skin = FindOrThrow(request.SkinId);
            if (skin.Category != "PLAYER")
                throw new InvalidSkinCategoryException(skin.Category);

            var loadout = LoadoutStore.FirstOrDefault(l => l.PlayerId == playerId);
            if (loadout is not null)
                loadout.SkinId = skin.Id;
            else
                LoadoutStore.Add(new PlayerLoadoutEntity { PlayerId = playerId, SkinId = skin.Id });

            return Task.FromResult(new PlayerLoadoutResponse(playerId, skin.Id, skin.Name, skin.AssetKey));
        }
    }

    public Task<PlayerLoadoutResponse> GetPlayerLoadoutAsync(int playerId)
    {
        lock (Lock)
        {
            var loadout = LoadoutStore.FirstOrDefault(l => l.PlayerId == playerId);
            if (loadout is null)
                return Task.FromResult(new PlayerLoadoutResponse(playerId, null, null, null));

            var skin = SkinStore.FirstOrDefault(s => s.Id == loadout.SkinId);
            return Task.FromResult(new PlayerLoadoutResponse(playerId, skin?.Id, skin?.Name, skin?.AssetKey));
        }
    }

    public Task<TournamentBackgroundResponse> SetTournamentBackgroundAsync(int tournamentId, SetTournamentBackgroundRequest request)
    {
        lock (Lock)
        {
            var skin = FindOrThrow(request.SkinId);
            if (skin.Category != "BACKGROUND")
                throw new InvalidSkinCategoryException(skin.Category);

            var bg = BackgroundStore.FirstOrDefault(b => b.TournamentId == tournamentId);
            if (bg is not null)
                bg.SkinId = skin.Id;
            else
                BackgroundStore.Add(new TournamentBackgroundEntity { TournamentId = tournamentId, SkinId = skin.Id });

            return Task.FromResult(new TournamentBackgroundResponse(tournamentId, skin.Id, skin.Name, skin.AssetKey));
        }
    }

    public Task<TournamentBackgroundResponse> GetTournamentBackgroundAsync(int tournamentId)
    {
        lock (Lock)
        {
            var bg = BackgroundStore.FirstOrDefault(b => b.TournamentId == tournamentId);
            if (bg is null)
                return Task.FromResult(new TournamentBackgroundResponse(tournamentId, null, null, null));

            var skin = SkinStore.FirstOrDefault(s => s.Id == bg.SkinId);
            return Task.FromResult(new TournamentBackgroundResponse(tournamentId, skin?.Id, skin?.Name, skin?.AssetKey));
        }
    }

    // Must be called under Lock
    private static SkinEntity FindOrThrow(int id)
        => SkinStore.FirstOrDefault(s => s.Id == id) ?? throw new SkinNotFoundException(id);

    private static SkinResponse Map(SkinEntity e)
        => new(e.Id, e.Category, e.Name, e.AssetKey, e.IsPremium, e.IsActive, e.CreatedAt);

    private class SkinEntity
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public string AssetKey { get; set; } = "";
        public bool IsPremium { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class PlayerLoadoutEntity
    {
        public int PlayerId { get; set; }
        public int SkinId { get; set; }
    }

    private class TournamentBackgroundEntity
    {
        public int TournamentId { get; set; }
        public int SkinId { get; set; }
    }
}
