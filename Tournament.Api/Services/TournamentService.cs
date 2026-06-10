using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class TournamentService : ITournamentService
{
    public Task<TournamentResponse> CreateTournamentAsync(CreateTournamentRequest request)
        => throw new NotImplementedException();

    public Task<TournamentResponse> GetTournamentAsync(int id)
        => throw new NotImplementedException();

    public Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync()
        => throw new NotImplementedException();

    public Task<TournamentResponse> UpdateTournamentStatusAsync(int id, UpdateTournamentStatusRequest request)
        => throw new NotImplementedException();
}
