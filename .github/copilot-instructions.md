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

There are no test projects and no lint/analyzer configuration currently.

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

### Aspire & ServiceDefaults

`Letters.ServiceDefaults/Extensions.cs` exposes `AddServiceDefaults<TBuilder>()`, called from `Program.cs` in any service project. It wires up:
- OpenTelemetry (logs, metrics, traces → OTLP exporter)
- Health check endpoints (`/health`, `/alive`)
- HTTP client resilience (retry + circuit breaker)
- Service discovery

`Letters.AppHost/AppHost.cs` currently registers only the `letters` project. Add databases, queues, or other services here using Aspire hosting integrations (`builder.AddSqlServer(...)`, etc.) and pass resource references to dependent services.

## Key Conventions

- **Nullable reference types** and **implicit usings** are enabled across all projects.
- New Razor pages go in `Components/Pages/`; add a `@page "/route"` directive and optionally `@attribute [Authorize]`.
- When adding a new HTTP client, register it with `.AddHttpClient(...).AddStandardResilienceHandler()` to get resilience policies from ServiceDefaults.
- When adding new Aspire-orchestrated resources (e.g., SQL Server, Redis), define them in `Letters.AppHost/AppHost.cs` and inject the connection via `builder.AddProject<Projects.Letters>("letters").WithReference(resource)`.
- `IdentityNoOpEmailSender` is a dev-only stub — replace it with a real implementation before enabling email-required flows.
- The scaffolded Identity UI in `Components/Account/` is editable; treat it as regular application code.
