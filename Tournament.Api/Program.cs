using Tournament.Api.Contracts;
using Tournament.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDuelService, DuelService>();
builder.Services.AddScoped<IScoreService, ScoreService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
