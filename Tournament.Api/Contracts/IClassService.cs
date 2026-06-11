using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IClassService
{
    Task<IEnumerable<ClassResponse>> GetAllClassesAsync();
    Task<ClassResponse> GetClassAsync(int id);
    Task<IEnumerable<SkillResponse>> GetClassSkillsAsync(int classId);
    Task<SkillResponse> GetSkillAsync(int id);
}
