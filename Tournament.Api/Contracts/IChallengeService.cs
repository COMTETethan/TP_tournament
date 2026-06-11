using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IChallengeService
{
    Task<ChallengeResponse> CreateChallengeAsync(CreateChallengeRequest request);
    Task<IEnumerable<ChallengeResponse>> GetUserChallengesAsync(int userId);
    Task<ChallengeResponse> AcceptChallengeAsync(int challengeId);
    Task<ChallengeResponse> DeclineChallengeAsync(int challengeId);
}
