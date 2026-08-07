using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration["OS_DATABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "No database connection string configured. Set OS_DATABASE_CONNECTION_STRING or ConnectionStrings:Default.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Liveness/readiness probe for local docker-compose and, later, Container Apps.
app.MapHealthChecks("/health");

app.MapGet("/api/v1/health", (IHostEnvironment env) => Results.Ok(new
{
    status = "healthy",
    environment = env.EnvironmentName,
    version = "0.0.1"
}));

app.Run();

public partial class Program { }
