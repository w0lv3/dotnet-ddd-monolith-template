# Simplified Layered DDD Monolith Template

A production-oriented ASP.NET Core template for a modular monolith using simplified layered Domain-Driven Design. It favors explicit application services and repository interfaces over CQRS, MediatR, vertical slices, generic repositories, or microservices.

See [Architecture](docs/architecture.md) for the design rationale and detailed feature workflow.

## Prerequisites

- .NET SDK 10.0.300 or a compatible .NET 10 feature band
- Docker with Compose for local dependencies and PostgreSQL integration tests
- A LocalStack auth token for the licensed local Cognito service
- A POSIX shell and `curl`; `jq` is also required by the identity smoke scripts

## Quick Start

Except for [Template Authoring](#template-authoring), run every command from the source or generated project root. In a generated project, `Example` in paths and names is replaced with the name supplied to `dotnet new`.

Create a local Compose environment file and set `LOCALSTACK_AUTH_TOKEN` in it:

```bash
cp .env.example .env
```

Start PostgreSQL, Keycloak, and LocalStack Cognito, then apply the database migrations:

```bash
docker compose -f docker-compose.local.yml up -d --wait
export ConnectionStrings__Database='Host=localhost;Port=5433;Database=app;Username=postgres;Password=postgres'
bash scripts/migrate.sh
```

Run the API with the default Development Keycloak configuration:

```bash
dotnet run --project src/Example.Api/Example.Api.csproj
```

The default HTTP launch URL is `http://localhost:5075`. Development OpenAPI documents are available at `/openapi/v1.json` and `/openapi/v1.yaml`.

## Project Structure

```text
Example.slnx
src/
├── Example.Api/
├── Example.Application/
├── Example.Domain/
└── Example.Infrastructure/
tests/
├── Example.Api.IntegrationTests/
├── Example.Architecture.Tests/
├── Example.Application.Tests/
├── Example.Domain.Tests/
└── Example.Infrastructure.IntegrationTests/
```

The dependency direction is inward:

```text
Api ───────────────► Application
 │                         │
 └──► Infrastructure ──────┴──► Domain
```

- **Domain** owns entities, value objects, invariants, and behavior without framework dependencies.
- **Application** owns use cases, service and repository interfaces, DTOs, validation, and mapping.
- **Infrastructure** implements persistence and other external integrations with EF Core and PostgreSQL.
- **API** owns HTTP models, controllers, authentication, authorization, ProblemDetails, OpenAPI, and composition.

Architecture tests enforce project references, forbidden dependencies, framework isolation, and key type ownership.

## Build And Test

Build or run the complete xUnit suite from either project root:

```bash
dotnet build
dotnet test
```

Docker must be running for the PostgreSQL-backed Infrastructure and API integration tests. Run one project when a narrower check is sufficient:

```bash
dotnet test tests/Example.Domain.Tests/Example.Domain.Tests.csproj
dotnet test tests/Example.Application.Tests/Example.Application.Tests.csproj
dotnet test tests/Example.Infrastructure.IntegrationTests/Example.Infrastructure.IntegrationTests.csproj
dotnet test tests/Example.Api.IntegrationTests/Example.Api.IntegrationTests.csproj
dotnet test tests/Example.Architecture.Tests/Example.Architecture.Tests.csproj
```

## Configuration And Secrets

Configuration is grouped under `ConnectionStrings`, `Authentication`, `OpenApi`, and `Logging`. With the default ASP.NET Core host, later sources override earlier ones: `appsettings.json`, environment-specific appsettings, Development user-secrets, environment variables, then command-line arguments.

Committed settings contain placeholders or local-only defaults. Store developer secrets with user-secrets:

```bash
dotnet user-secrets set 'ConnectionStrings:Database' 'Host=localhost;Port=5433;Database=app;Username=postgres;Password=postgres' --project src/Example.Api/Example.Api.csproj
```

Use double underscores for nested environment keys, for example `ConnectionStrings__Database` and `Authentication__Provider`. The API reads user-secrets in Development; EF design-time commands deliberately require the `ConnectionStrings__Database` environment variable.

Docker Compose reads `.env` automatically. The .NET application does not load `.env`; export API variables in the shell or use user-secrets. Never commit `.env`, cloud credentials, client secrets, tokens, or production connection strings.

## Local Dependencies

`docker-compose.local.yml` is the standalone local dependency stack. It starts PostgreSQL on `5433`, Keycloak on `8080`, and LocalStack on `4566`; the API runs separately with `dotnet run`. The LocalStack Cognito service is licensed, so Compose configuration fails unless `LOCALSTACK_AUTH_TOKEN` is set in the environment or `.env`.

```bash
docker compose -f docker-compose.local.yml up -d --wait
docker compose -f docker-compose.local.yml ps
docker compose -f docker-compose.local.yml logs -f keycloak
docker compose -f docker-compose.local.yml down
```

Reset all persisted PostgreSQL, Keycloak, and LocalStack data when deterministic initialization is needed:

```bash
docker compose -f docker-compose.local.yml down --volumes
```

Ports and local-only credentials can be changed with the variables shown in `.env.example`. Keep the API connection string and provider URLs aligned with any port overrides.

## Persistence And Migrations

Persistence belongs to Infrastructure. `ApplicationDbContext`, entity configurations, repository implementations, and migrations use EF Core with PostgreSQL. Migrations are explicit and never run during API startup.

Apply existing migrations:

```bash
export ConnectionStrings__Database='Host=localhost;Port=5433;Database=app;Username=postgres;Password=postgres'
bash scripts/migrate.sh
```

Create a migration after changing persistence mappings:

```bash
dotnet tool restore
dotnet ef migrations add AddFeature \
  --project src/Example.Infrastructure/Example.Infrastructure.csproj \
  --startup-project src/Example.Infrastructure/Example.Infrastructure.csproj \
  --output-dir Persistence/Migrations
```

## Authentication And Authorization

Select exactly one JWT bearer provider with `Authentication__Provider`: `Keycloak`, `Cognito`, or `EntraId`. Only the selected provider is validated at startup. Production validation checks signature, issuer, token lifetime, and audience when configured. HTTP metadata and LocalStack are restricted to safe Development scenarios.

The example API uses provider-independent `examples.read` and `examples.write` policies. Permissions may come from `scope`/`scp`, `roles`, or normalized provider claims. Application code can depend on `ICurrentUser`; `HttpContext` and provider-specific claim handling remain in the API boundary.

### Keycloak

Keycloak is the default Development provider. The imported realm contains local-only test credentials. With the dependency stack running, retrieve a read token:

```bash
ACCESS_TOKEN=$(curl --fail --silent --show-error \
  --request POST http://localhost:8080/realms/example/protocol/openid-connect/token \
  --data grant_type=password \
  --data client_id=example-test \
  --data client_secret=local-test-secret \
  --data username=developer \
  --data 'password=Developer123!' \
  --data scope=examples.read | jq -er '.access_token')

curl --fail --header "Authorization: Bearer $ACCESS_TOKEN" \
  http://localhost:5075/api/examples
```

These realm credentials are only for local development and must not be reused elsewhere.

### AWS Cognito And LocalStack

For AWS Cognito, configure `Region`, `UserPoolId`, and `ClientId` under `Authentication:Cognito`; `Authority` is derived when omitted. `Audience` is optional, and `ResourceServerIdentifier` removes prefixes such as `example-api/` from custom scopes. Only access tokens with the configured `client_id` are accepted.

LocalStack creates deterministic local pool, client, resource server, scopes, groups, and user data. Start the standalone stack, run the API with its local profile, then execute the smoke check in another shell:

```bash
dotnet run --project src/Example.Api/Example.Api.csproj --launch-profile cognito-local
bash scripts/smoke-localstack-cognito.sh
```

`Authentication:Cognito:UseLocalStack=true`, HTTP issuer metadata, and the local JWKS URI are rejected outside Development.

### Microsoft Entra ID

Register a single-tenant web API and expose delegated scopes or application roles named `examples.read` and `examples.write`. Configure `Authentication__EntraId__TenantId`, `ClientId`, `Instance`, and optionally `Audience`, then set `Authentication__Provider=EntraId`. Delegated permissions use `scp`; application permissions use `roles`. Current-user identity prefers `oid` over `sub`, and email falls back to `preferred_username`.

## OpenAPI

OpenAPI is enabled by `OpenApi__Enabled` and defaults to Development only. Controllers, API models, and endpoint metadata are the source of truth. Generate the checked-in OpenAPI 3.1 YAML rather than editing `openapi/openapi.yaml`:

```bash
sh scripts/generate-openapi.sh
sh scripts/generate-openapi.sh --check
```

The script builds and starts the API on an isolated local port before downloading `/openapi/v1.yaml`.

## Adding A Feature

Add behavior from the inside out: Domain model and tests, Application models/interfaces/service/validation/mapping, Infrastructure repository and EF configuration, then API request/response models and a thin controller. Add a migration for schema changes and test at the narrowest appropriate layer. The complete path is documented in [Adding A Feature](docs/architecture.md#adding-a-feature).

## Troubleshooting

- Compose reports `LOCALSTACK_AUTH_TOKEN` is required: set a valid token in `.env` or export it before every Compose command.
- `scripts/migrate.sh` reports a missing connection string: export `ConnectionStrings__Database`; user-secrets are not read by the design-time factory.
- PostgreSQL, Keycloak, or LocalStack cannot bind: override `POSTGRES_PORT`, `KEYCLOAK_PORT`, or `LOCALSTACK_PORT` and update API configuration accordingly.
- Authentication startup fails: configure all required values for the provider selected by `Authentication__Provider`; unused providers may remain empty.
- Integration tests cannot start PostgreSQL: ensure Docker is running and available to Testcontainers.
- Local identity initialization is stale: run `docker compose -f docker-compose.local.yml down --volumes`, then start the stack again.

## Template Authoring

These commands are only for a template author or contributor and run from the template repository root:

```bash
dotnet new install .
dotnet new ddd-monolith -n Cinema
```

The generated project is in `Cinema/`. Change to that directory before using the root-level build, test, Compose, migration, and run commands documented above:

```bash
cd Cinema
dotnet build
dotnet test
```

Re-run `dotnet new install .` after changing the local template. Uninstall that local source with its absolute path:

```bash
dotnet new uninstall /absolute/path/to/dotnet-ddd-monolith-template
```
