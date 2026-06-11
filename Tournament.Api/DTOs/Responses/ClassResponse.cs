namespace Tournament.Api.DTOs.Responses;

/// <summary>A champion class (e.g. Knight, Mage) and how many skills it owns.</summary>
public record ClassResponse(int Id, string Name, string Description, int SkillCount);
