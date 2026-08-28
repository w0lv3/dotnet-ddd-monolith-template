# Simplified Layered DDD Monolith Template

A reusable ASP.NET Core template for building a monolith with simplified layered Domain-Driven Design.

## Prerequisites

- .NET SDK 10.0.300 or a compatible .NET 10 feature band
- Docker for local PostgreSQL and Infrastructure integration tests

## Install

From the repository root:

```bash
dotnet new install .
```

To install an updated local version, run the same command again.

## Generate

```bash
dotnet new ddd-monolith -n Cinema
```

The command creates a `Cinema` directory containing:

```text
Cinema.slnx
src/
├── Cinema.Api/
├── Cinema.Application/
├── Cinema.Domain/
└── Cinema.Infrastructure/
tests/
├── Cinema.Api.IntegrationTests/
├── Cinema.Architecture.Tests/
├── Cinema.Application.Tests/
├── Cinema.Domain.Tests/
└── Cinema.Infrastructure.IntegrationTests/
```

## Build And Test

```bash
dotnet build Cinema/Cinema.slnx
dotnet test Cinema/Cinema.slnx
```

## Local PostgreSQL

Start PostgreSQL and apply migrations from the generated project directory:

```bash
docker compose up -d --wait postgres
bash scripts/migrate.sh
```

Run the API:

```bash
dotnet run --project src/Cinema.Api/Cinema.Api.csproj
```

Stop local infrastructure:

```bash
docker compose down
```

The committed PostgreSQL credentials are for local development only. Override the database connection string in other environments with `ConnectionStrings__Database`.
The local container exposes PostgreSQL on host port `5433` to avoid common conflicts with an existing PostgreSQL installation on `5432`.

Migrations are applied explicitly and are not run automatically during application startup.

## Uninstall

```bash
dotnet new uninstall /path/to/dotnet-ddd-monolith-template
```
