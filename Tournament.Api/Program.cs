using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Tournament.Api.Contracts;
using Tournament.Api.Data;
using Tournament.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default not configured.");

builder.Services.AddDbContext<TournamentDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Controllers & Swagger ─────────────────────────────────────────────────────
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

// ── JWT Authentication ────────────────────────────────────────────────────────
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

// ── CORS ──────────────────────────────────────────────────────────────────────
const string SpaCorsPolicy = "SpaCors";
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.WithOrigins(
                "http://localhost:5173", "http://localhost:5174", "http://localhost:5175",
                "http://localhost:5176", "http://localhost:5177", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Services (DB-backed: Scoped; in-memory: Singleton) ────────────────────────
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDuelService, DuelService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddSingleton<IReplayService, ReplayService>();
builder.Services.AddSingleton<ISkinService, SkinService>();
builder.Services.AddSingleton<ISeasonService, SeasonService>();
builder.Services.AddSingleton<IBattlepassService, BattlepassService>();
builder.Services.AddSingleton<IObjectiveService, ObjectiveService>();
builder.Services.AddSingleton<ISeasonRewardService, SeasonRewardService>();
builder.Services.AddSingleton<IClassService, ClassService>();
builder.Services.AddSingleton<ICombatService, CombatService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

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
