using System.Reflection;
using Microsoft.OpenApi;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.Services;
using Tournament.Api.Services.Db;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Tournament API",
        Version     = "v1",
        Description = "API de gestion de tournois : joueurs, duels, scores, saisons, battlepass et récompenses."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

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

// ── CORS (allow the React/Vite SPA + browser clients in dev) ──────────────────
const string SpaCorsPolicy = "SpaCors";
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.WithOrigins(
                "http://localhost:5173", "http://localhost:5174", "http://localhost:5175",
                "http://localhost:5176", "http://localhost:5177", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Core persistence: PostgreSQL when ConnectionStrings:Default is set, else in-memory ──
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true; // snake_case columns → record params
    builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<ITournamentService, DbTournamentService>();
    builder.Services.AddSingleton<IPlayerService, DbPlayerService>();
    builder.Services.AddSingleton<ITournamentPlayerService, DbTournamentPlayerService>();
    builder.Services.AddSingleton<IDuelService, DbDuelService>();
    // The combat engine stays in-memory but is written through to the database.
    builder.Services.AddSingleton<ICombatService>(sp =>
        new DbCombatPersistenceDecorator(new CombatService(), sp.GetRequiredService<NpgsqlDataSource>()));
}
else
{
    builder.Services.AddSingleton<ITournamentService, TournamentService>();
    builder.Services.AddSingleton<IPlayerService, PlayerService>();
    builder.Services.AddSingleton<ITournamentPlayerService, TournamentPlayerService>();
    builder.Services.AddSingleton<IDuelService, DuelService>();
    builder.Services.AddSingleton<ICombatService, CombatService>();
}

// Computed over the stores above (DI picks the injecting constructor).
builder.Services.AddSingleton<IScoreService, ScoreService>();
builder.Services.AddSingleton<IDuelCombatService, DuelCombatService>();

// Read-only catalog + modules that remain in-memory for now.
builder.Services.AddSingleton<IClassService, ClassService>();
builder.Services.AddSingleton<IReplayService, ReplayService>();
builder.Services.AddSingleton<ISkinService, SkinService>();
builder.Services.AddSingleton<ISeasonService, SeasonService>();
builder.Services.AddSingleton<IBattlepassService, BattlepassService>();
builder.Services.AddSingleton<IObjectiveService, ObjectiveService>();
builder.Services.AddSingleton<ISeasonRewardService, SeasonRewardService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();

// Don't force HTTPS in Development: the SPA talks to http://localhost:5000 and a
// 307 redirect to a self-signed https port breaks browser fetch/XHR (and the SSR
// loader fetches). Keep the redirect for non-dev environments.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(SpaCorsPolicy);
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tournament API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
