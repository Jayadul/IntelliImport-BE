using IntelliImport.API.Middleware;
using IntelliImport.Application.Features.Extractions.Commands;
using IntelliImport.Infrastructure;
using IntelliImport.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IntelliImport API", Version = "v1" });
});

// Kestrel timeouts for long-running AI operations
// These must be >= HttpClient timeout >= Polly TotalRequestTimeout
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(200);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(200);
});

// Infrastructure (EF, Ollama, PdfPig, Polly)
builder.Services.AddInfrastructure(builder.Configuration);

// MediatR — scan Application assembly for handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ProcessPdfCommand>());

// CORS — allow Angular dev server
builder.Services.AddCors(opts => opts.AddPolicy("Angular", p =>
    p.WithOrigins("http://localhost:4200", "http://localhost:4201")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

// After services are configured, before app.Build()
var ollama = builder.Configuration.GetSection("Ollama");
builder.Logging.AddConsole();

var app = builder.Build();

// Log configuration info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation(
    "Ollama configured: BaseUrl={Url}, Model={Model}, Timeout={Timeout}s",
    ollama["BaseUrl"],
    ollama["Model"],
    ollama["TimeoutSecs"]);

// ── Auto-migrate on startup ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Pipeline ───────────────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Angular");
app.MapControllers();
app.Run();
