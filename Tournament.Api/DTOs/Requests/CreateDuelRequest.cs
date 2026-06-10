namespace Tournament.Api.DTOs.Requests;

public record CreateDuelRequest(int Player1Id, int Player2Id, int DuelOrder);
