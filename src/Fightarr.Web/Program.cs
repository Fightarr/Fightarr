using Fightarr.Core.Configuration;
using Fightarr.Core.Services;
using Fightarr.Data;
using Fightarr.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=fightarr.db";

builder.Services.AddDbContext<FightarrDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure settings
builder.Services.Configure<FightarrSettings>(
    builder.Configuration.GetSection("Fightarr"));
builder.Services.Configure<MetadataProviderSettings>(
    builder.Configuration.GetSection("MetadataProvider"));

// Register repositories
builder.Services.AddScoped<IFightEventRepository, FightEventRepository>();

// Register services
builder.Services.AddHttpClient<IMetadataService, MetadataService>();
builder.Services.AddScoped<IReleaseParsingService, ReleaseParsingService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is available"));
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FightarrDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

Log.Information("Starting Fightarr web application");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
