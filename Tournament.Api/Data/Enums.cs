using NpgsqlTypes;

namespace Tournament.Api.Data;

public enum TournamentStatus
{
    [PgName("OPEN")]        OPEN,
    [PgName("IN_PROGRESS")] IN_PROGRESS,
    [PgName("CLOSED")]      CLOSED
}

public enum DuelOutcome
{
    [PgName("PLAYER1_WIN")] PLAYER1_WIN,
    [PgName("PLAYER2_WIN")] PLAYER2_WIN,
    [PgName("DRAW")]        DRAW
}
