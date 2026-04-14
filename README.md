# IntelliImport — Backend (.NET 8)

## Architecture
```
IntelliImport-BE/
└── src/
    ├── Domain/            Pure domain: entities, enums, interfaces, Result<T>
    ├── Application/       CQRS handlers (MediatR), abstractions, DTOs
    ├── Infrastructure/    EF Core, Ollama/PdfPig/Polly implementations
    └── API/               ASP.NET Core Web API, controllers, middleware
```

## Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or full)
- Ollama running locally with `phi3` pulled: `ollama pull phi3`

## Run
```bash
cd src/API
dotnet run
# API: https://localhost:5001  |  Swagger: https://localhost:5001/swagger
```

## Configuration
Edit `appsettings.Development.json`:
- `ConnectionStrings:DefaultConnection` — SQL Server connection string
- `Ollama:BaseUrl` — Ollama endpoint (default: http://localhost:11434)
- `Ollama:Model` — Model name (default: phi3)

## EF Core Migrations
```bash
# From solution root
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure \
  --startup-project src/API \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/API
```
Migrations also run automatically on startup in Development mode.

## API Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/extractions/upload` | Upload PDF, trigger AI extraction |
| GET | `/api/extractions` | Paginated extraction history |
| GET | `/api/extractions/{id}` | Get extraction detail by ID |
| POST | `/api/extractions/{id}/approve-sync` | Approve & simulate ERP sync |
| DELETE | `/api/extractions/{id}` | Delete extraction record |

## Result Pattern
All handlers return `Result<T>` (generic) or `Result` (non-generic, commands).
```csharp
var result = await mediator.Send(command, ct);
if (result.IsSuccess) return Ok(result.Value);
return BadRequest(new { error = result.Error, code = result.ErrorCode });
```

## Resilience (Polly)
The Ollama `HttpClient` uses `AddStandardResilienceHandler` with:
- Retry: 3 attempts, exponential back-off (1s → 2s → 4s) with jitter
- Circuit breaker: opens after 5 failures in 30s, stays open 15s
- Total timeout: 120s per request
