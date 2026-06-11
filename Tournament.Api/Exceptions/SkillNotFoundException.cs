namespace Tournament.Api.Exceptions;

public class SkillNotFoundException : Exception
{
    public int SkillId { get; }

    public SkillNotFoundException(int skillId)
        : base($"Skill with id {skillId} was not found.")
    {
        SkillId = skillId;
    }
}
