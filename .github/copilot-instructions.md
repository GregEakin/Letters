# Copilot Instructions

## Solution Overview

**Letters** is a Blazor Server web application (.NET 10) with ASP.NET Core Identity, orchestrated via .NET Aspire. It targets SQL Server via EF Core.

Three projects:
- **Letters** — Blazor Server app (UI, Identity, EF Core)
- **Letters.AppHost** — Aspire orchestration host; run this to launch the full environment
- **Letters.ServiceDefaults** — Shared extension methods for OpenTelemetry, health checks, HTTP resilience, and service discovery

## Build & Run

```powershell
# Run the full environment (preferred — starts Aspire dashboard + app)
dotnet run --project Letters.AppHost

# Build the solution
dotnet build Letters.slnx

# Run the app directly (no Aspire orchestration)
dotnet run --project Letters
```

```powershell
# Run all tests
dotnet test Letters.Tests/Letters.Tests

# Run a single test by name
dotnet test Letters.Tests/Letters.Tests --filter "FullyQualifiedName~TestMethodName"
```

Tests use **TUnit** (not xUnit or NUnit). There is no lint/analyzer configuration.

## EF Core Migrations

```powershell
# Add a migration (run from repo root)
dotnet ef migrations add <MigrationName> --project Letters --startup-project Letters

# Apply migrations
dotnet ef database update --project Letters --startup-project Letters
```

## Architecture

### Blazor Component Layout

All Razor components live in `Letters/Components/`, organized by role:

```
Components/
├── Pages/        # Routable pages (@page directive)
├── Layout/       # Shell components (MainLayout, NavMenu)
├── Account/      # Scaffolded ASP.NET Identity UI (Login, Register, Manage, 2FA, etc.)
└── App.razor     # Root component
```

Pages use `@attribute [Authorize]` for route-level protection. Conditional rendering uses `<AuthorizeView>`.

### Data Layer

`Letters/Data/` contains:
- `ApplicationDbContext` — EF Core DbContext (extends `IdentityDbContext<ApplicationUser>`)
- `ApplicationUser` — Identity user model (extend this to add user profile fields)

The connection string `DefaultConnection` is read from configuration; in development it points to LocalDB. User secrets are configured for both Letters and Letters.AppHost (`UserSecretsId` set in each `.csproj`).

### Azure Storage

The app uses **Azure Blob Storage** and **Azure Table Storage** via Aspire integrations:
- Registered in `Program.cs` with `builder.AddAzureBlobServiceClient("blobs")` and `builder.AddAzureTableServiceClient("tables")`
- In development, Aspire runs both as a local emulator (`RunAsEmulator()` in `AppHost.cs`)
- Inject `BlobServiceClient` or `TableServiceClient` directly where needed

### Aspire & ServiceDefaults

`Letters.ServiceDefaults/Extensions.cs` exposes `AddServiceDefaults()`, called from `Program.cs`. It wires up:
- OpenTelemetry (logs, metrics, traces → OTLP exporter)
- Health check endpoints (`/health`, `/alive`)
- HTTP client resilience (retry + circuit breaker)
- Service discovery

`Letters.AppHost/AppHost.cs` orchestrates the `letters` project and Azure Storage resources. Add new resources here and pass references to dependent services via `.WithReference(resource)`.

### Test Architecture

`Letters.Tests/` contains two projects:
- **`Letters.Tests/WebApp`** — A minimal ASP.NET Core API used only as a test fixture (has a `/ping` endpoint). This is **not** the main Blazor app.
- **`Letters.Tests/Letters.Tests`** — TUnit test project that creates a `WebApplicationFactory<Program>` against `WebApp`, not the main `Letters` project.

New integration tests should follow the same pattern: add endpoints to `WebApp`, then test them via `WebApplicationFactory.CreateClient()`.

## Key Conventions

- **Nullable reference types** and **implicit usings** are enabled across all projects.
- New Razor pages go in `Components/Pages/`; add a `@page "/route"` directive and optionally `@attribute [Authorize]`.
- When adding a new HTTP client, register it with `.AddHttpClient(...).AddStandardResilienceHandler()` to get resilience policies from ServiceDefaults.
- When adding new Aspire-orchestrated resources (e.g., SQL Server, Redis), define them in `Letters.AppHost/AppHost.cs` and inject the connection via `builder.AddProject<Projects.Letters>("letters").WithReference(resource)`.
- `IdentityNoOpEmailSender` is a dev-only stub — replace it with a real implementation before enabling email-required flows.
- The scaffolded Identity UI in `Components/Account/` is editable; treat it as regular application code.
- `BlazorDisableThrowNavigationException` is set to `true` in `Letters.csproj` — navigation exceptions are suppressed rather than thrown.
