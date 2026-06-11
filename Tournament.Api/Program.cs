using Tournament.Api.Contracts;
using Tournament.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDuelService, DuelService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IReplayService, ReplayService>();
builder.Services.AddScoped<ISkinService, SkinService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
