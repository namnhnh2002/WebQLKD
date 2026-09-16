using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NamIT.Business.Api.Middleware;
using NamIT.Business.Infrastructure;
using NamIT.Business.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPort, out var port) && port > 0)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be supplied in production.");
    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt__SecretKey must be supplied in production.");
}

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NamIT Business API", Version = "v1" });
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token dạng: Bearer {token}"
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// ---------- JWT Authentication ----------
var requiredJwtSecret = jwtSecret!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(requiredJwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

// ---------- CORS ----------
var configuredOrigins = builder.Environment.IsProduction()
    ? Array.Empty<string>()
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var environmentOrigins = builder.Configuration["Cors:AllowedOriginsCsv"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();
var allowedOrigins = configuredOrigins.Concat(environmentOrigins).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors__AllowedOriginsCsv must be supplied in production.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue<bool>("UseHttpsRedirection"))
{
    app.UseHttpsRedirection();
}
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/db", async (ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ok" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database health check failed.");
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

// ---------- Initialize + Seed ----------
var initializeDatabase = builder.Environment.IsProduction()
    || builder.Configuration.GetValue<bool>("Database:InitializeOnStartup");
if (initializeDatabase)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        var enableDemoAdminFallback = !app.Environment.IsProduction()
            && builder.Configuration.GetValue<bool>("Database:EnableDemoAdminFallback");
        await DbSeeder.SeedAsync(db, enableDemoAdminFallback, builder.Configuration["Database:DemoAdminPassword"]);
    }
    catch (Exception ex)
    {
        if (app.Environment.IsProduction())
            throw;
        app.Logger.LogWarning(ex, "Database is unavailable. Local startup continues without database initialization.");
    }
}

app.Run();

// Cho phép WebApplicationFactory<Program> dùng trong integration test (NamIT.Business.Tests)
public partial class Program { }
