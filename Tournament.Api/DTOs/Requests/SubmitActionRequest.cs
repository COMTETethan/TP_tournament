namespace Tournament.Api.DTOs.Requests;

/// <summary>
/// One champion (identified by its slot, 1 or 2) submits the skill it will use this turn.
/// The turn is resolved server-side once both champions have submitted their action.
/// </summary>
public record SubmitActionRequest(int Slot, int SkillId);
