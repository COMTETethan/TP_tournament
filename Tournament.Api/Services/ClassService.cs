using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>Read-only access to the seeded class/skill roster (see <see cref="ClassCatalog"/>).</summary>
public class ClassService : IClassService
{
    public Task<IEnumerable<ClassResponse>> GetAllClassesAsync()
        => Task.FromResult<IEnumerable<ClassResponse>>(
            ClassCatalog.Classes.Select(MapClass).ToList());

    public Task<ClassResponse> GetClassAsync(int id)
    {
        var def = ClassCatalog.FindClass(id) ?? throw new ClassNotFoundException(id);
        return Task.FromResult(MapClass(def));
    }

    public Task<IEnumerable<SkillResponse>> GetClassSkillsAsync(int classId)
    {
        if (!ClassCatalog.ClassExists(classId))
            throw new ClassNotFoundException(classId);

        return Task.FromResult<IEnumerable<SkillResponse>>(
            ClassCatalog.SkillsForClass(classId).Select(MapSkill).ToList());
    }

    public Task<SkillResponse> GetSkillAsync(int id)
    {
        var def = ClassCatalog.FindSkill(id) ?? throw new SkillNotFoundException(id);
        return Task.FromResult(MapSkill(def));
    }

    private static ClassResponse MapClass(ClassDef c)
        => new(c.Id, c.Name, c.Description, ClassCatalog.SkillsForClass(c.Id).Count());

    private static SkillResponse MapSkill(SkillDef s)
        => new(s.Id, s.ClassId, s.Name, s.Category, s.Power, s.Duration, s.AuraEffect, s.Description);
}
