namespace Tournament.Api.DTOs.Responses;

/// <summary>A minimal public view of a registered user (no secrets).</summary>
public record UserSummaryResponse(int Id, string Email);
