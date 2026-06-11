using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tournament.Api.Contracts;
using Tournament.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ── JWT Authentication ───────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDuelService, DuelService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IReplayService, ReplayService>();
builder.Services.AddScoped<ISkinService, SkinService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IBattlepassService, BattlepassService>();
builder.Services.AddScoped<IObjectiveService, ObjectiveService>();
builder.Services.AddScoped<ISeasonRewardService, SeasonRewardService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
