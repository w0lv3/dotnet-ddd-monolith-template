# Simplified Layered DDD Monolith Template

A reusable ASP.NET Core template for building a monolith with simplified layered Domain-Driven Design.

## Prerequisites

- .NET SDK 10.0.300 or a compatible .NET 10 feature band

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

## Uninstall

```bash
dotnet new uninstall /path/to/dotnet-ddd-monolith-template
```
