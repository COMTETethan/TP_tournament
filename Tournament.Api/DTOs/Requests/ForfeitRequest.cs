namespace Tournament.Api.DTOs.Requests;

/// <summary>The champion at the given slot (1 or 2) concedes the combat.</summary>
public record ForfeitRequest(int Slot);
