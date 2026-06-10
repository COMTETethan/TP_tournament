namespace Tournament.Api.DTOs.Requests;

/// <summary>Accepted values: PLAYER1_WIN, PLAYER2_WIN, DRAW</summary>
public record SetDuelOutcomeRequest(string Outcome);
