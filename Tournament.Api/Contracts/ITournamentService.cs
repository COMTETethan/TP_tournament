using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface ITournamentService
{
    Task<TournamentResponse> CreateTournamentAsync(CreateTournamentRequest request);
    Task<TournamentResponse> GetTournamentAsync(int id);
    Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync();
    Task<TournamentResponse> UpdateTournamentStatusAsync(int id, UpdateTournamentStatusRequest request);
}
