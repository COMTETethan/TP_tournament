using Microsoft.EntityFrameworkCore;
using Tournament.Api.Data.Entities;

namespace Tournament.Api.Data;

public class TournamentDbContext : DbContext
{
    public TournamentDbContext(DbContextOptions<TournamentDbContext> options) : base(options) { }

    public DbSet<TournamentEntity> Tournaments => Set<TournamentEntity>();
    public DbSet<PlayerEntity>     Players     => Set<PlayerEntity>();
    public DbSet<DuelEntity>       Duels       => Set<DuelEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TournamentEntity>(e =>
        {
            e.ToTable("tournaments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PlayerEntity>(e =>
        {
            e.ToTable("players");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.IsDisqualified).HasColumnName("is_disqualified");
            e.Property(x => x.PenaltyPoints).HasColumnName("penalty_points");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<DuelEntity>(e =>
        {
            e.ToTable("duels");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").IsRequired();
            e.Property(x => x.Player1Id).HasColumnName("player1_id").IsRequired();
            e.Property(x => x.Player2Id).HasColumnName("player2_id").IsRequired();
            e.Property(x => x.Outcome).HasColumnName("outcome");
            e.Property(x => x.DuelOrder).HasColumnName("duel_order").IsRequired();
            e.Property(x => x.PlayedAt).HasColumnName("played_at");
            e.Property(x => x.Duration).HasColumnName("duration");
        });
    }
}
