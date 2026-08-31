# Architecture

This template uses simplified layered Domain-Driven Design for a single deployable ASP.NET Core application. It keeps business rules independent from delivery and persistence details while avoiding the operational and conceptual cost of CQRS, MediatR, vertical slices, generic repositories, event buses, or microservices when ordinary application services are sufficient.

The design is intentionally explicit: controllers call application service interfaces, application services coordinate domain behavior and repository interfaces, and Infrastructure implements those interfaces with EF Core and PostgreSQL.

## Dependency Rules

Allowed production project references are:

```text
Example.Application ─────────────────────────► Example.Domain

Example.Infrastructure ─► Example.Application
Example.Infrastructure ──────────────────────► Example.Domain

Example.Api ─────────────► Example.Application
Example.Api ─────────────► Example.Infrastructure
```

The reverse directions are forbidden. Domain references no other production project; Application never references Infrastructure or API; Infrastructure never references API. The API references Infrastructure only as the composition root.

`Example.Architecture.Tests` verifies exact project references, assembly dependency direction, Domain and Application framework isolation, and ownership of controllers, validators, repositories, `DbContext`, and EF configurations. It also prevents controllers from depending on persistence implementations or exposing Domain entities as HTTP results.

## Request And Response Flow

```text
HTTP request
    │
    ▼
API controller + request model
    │ maps to
    ▼
Application model + service interface
    │
    ▼
Application service
    ├── validates input
    ├── invokes Domain behavior
    └── calls repository interface
              │
              ▼
Infrastructure repository
              │
              ▼
EF Core ApplicationDbContext ─► PostgreSQL

Domain entity
    │ maps to
    ▼
Application DTO
    │ maps to
    ▼
API response model ─► HTTP response
```

Cancellation tokens flow from controller actions through Application and repository calls to EF Core. Central exception handling translates validation, missing resources, domain failures, and persistence conflicts into ProblemDetails rather than scattering `try/catch` blocks across controllers.

## Layer Responsibilities

### Domain

`src/Example.Domain` owns entities, value objects, enums, domain exceptions, invariants, state transitions, and other business behavior. Entities expose meaningful methods instead of allowing callers to bypass invariants through unrestricted setters.

Domain is framework-independent. It must not contain EF Core mappings, ASP.NET Core types, AutoMapper, FluentValidation, HTTP concepts, identity-provider details, database calls, or DTO mapping.

### Application

`src/Example.Application` owns use-case orchestration:

- service interfaces and implementations
- repository and identity abstractions
- application models and DTOs
- FluentValidation validators for input constraints
- AutoMapper profiles from Domain entities to application DTOs
- application-level exceptions

Application services load entities, validate use-case input, invoke Domain behavior, persist through repository interfaces, and return DTOs. They do not access `DbContext`, `HttpContext`, provider SDKs, or API request/response models. Domain invariants remain in Domain rather than being replaced by input validation.

### Infrastructure

`src/Example.Infrastructure` owns EF Core, Npgsql, `ApplicationDbContext`, entity configurations, migrations, repository implementations, and provider configuration types needed by the API composition boundary. It implements abstractions owned by Application and may reference Domain for persistence mappings.

Infrastructure does not define business use cases or HTTP behavior. There is no generic repository or Unit of Work wrapper; the concrete repositories use EF Core directly and keep persistence details out of Application.

### API

`src/Example.Api` is the HTTP and composition boundary. It owns controllers, request and response models, explicit API mapping, JWT bearer setup, policy-based authorization, claim normalization, ProblemDetails, OpenAPI metadata, and `Program.cs`.

Controllers remain thin: bind an API model, map it to an Application model, call a service interface, map the DTO to a response, and return the appropriate status. They never query `ApplicationDbContext` or repository implementations directly and never return Domain entities.

Each layer owns its dependency registrations through `AddApplication`, `AddInfrastructure`, and `AddApi`; `Program.cs` composes them and `UseApi` keeps middleware ordering visible. Database migrations are not hidden in startup.

## Adding A Feature

Work from the business model outward and add only artifacts the feature needs:

1. Define or extend Domain entities, value objects, enums, exceptions, and behavior under `src/Example.Domain`.
2. Add pure Domain tests for invariants, boundaries, transitions, failure diagnostics, and state preservation.
3. Add Application models/DTOs, service and repository interfaces, validators, mappings, and an application service under `src/Example.Application`.
4. Add Application tests for orchestration, validation short-circuiting, repository interactions, mapping, and cancellation propagation.
5. Implement persistence under `src/Example.Infrastructure/Persistence`: repository, EF configuration, and `DbSet` changes.
6. Add and apply an EF migration when the relational model changes; verify it against PostgreSQL.
7. Add API request/response models and a thin controller under `src/Example.Api`, including authorization and OpenAPI metadata.
8. Add API integration coverage for routing, serialization, validation, ProblemDetails, status codes, authentication, authorization, and representative end-to-end behavior.
9. Regenerate `openapi/openapi.yaml` and run the architecture tests.

Do not create empty folders, speculative interfaces, generic repositories, or parallel feature patterns. Follow the existing `Examples` feature unless the domain requires a different shape.

## Authentication Abstraction

Keycloak, AWS Cognito, and Microsoft Entra ID converge on one JWT bearer and policy boundary in API:

```text
OIDC/OAuth provider ─► signed access token ─► JWT validation
                                              │
                                              ▼
                               examples.read / examples.write
                                              │
                                              ▼
                                    Application service
```

`Authentication:Provider` selects one provider, and only its required configuration is validated. Signature, issuer, lifetime, and configured audience validation remain enabled. Provider-specific scopes and roles are normalized to provider-independent permissions. Cognito additionally requires an access token and matching `client_id`; resource-server scope prefixes can be normalized. Entra delegated permissions use `scp`, while application permissions use `roles`.

Application owns the small `ICurrentUser` abstraction (`UserId`, `Email`, and `IsAuthenticated`). API implements it from `HttpContext`, preferring `oid` then `sub` for identity and `email` then `preferred_username` for email. Domain remains unaware of users, claims, HTTP, and identity providers unless identity becomes a genuine domain concept.

Development exceptions are narrow: local Keycloak may use HTTP metadata, and LocalStack Cognito may use an HTTP issuer and explicit JWKS URI only when `UseLocalStack` is enabled in Development. LocalStack is rejected outside Development. Secrets and provider credentials belong in user-secrets, environment variables, deployment configuration, or a secret manager.

## Persistence And Migrations

PostgreSQL is the production persistence model. EF entity mappings use `IEntityTypeConfiguration<TEntity>` in Infrastructure, keeping persistence annotations out of Domain. Repository interfaces live in Application; implementations live in Infrastructure. Read operations use no-tracking queries where mutation is unnecessary.

Migrations are explicit so startup failures and schema changes remain operational decisions. The design-time factory does not assume local credentials and requires `ConnectionStrings__Database`:

```bash
export ConnectionStrings__Database='Host=localhost;Port=5433;Database=app;Username=postgres;Password=postgres'
bash scripts/migrate.sh
```

To create a migration from the source or generated project root:

```bash
dotnet tool restore
dotnet ef migrations add AddFeature \
  --project src/Example.Infrastructure/Example.Infrastructure.csproj \
  --startup-project src/Example.Infrastructure/Example.Infrastructure.csproj \
  --output-dir Persistence/Migrations
```

Migration and mapping behavior is verified against disposable real PostgreSQL instances, not EF Core InMemory. The standalone `docker-compose.local.yml` supplies the developer database; it also includes Keycloak and licensed LocalStack Cognito, so `LOCALSTACK_AUTH_TOKEN` is required even when Compose first parses the stack.

## Testing Strategy

The five xUnit projects have distinct responsibilities:

| Project | Responsibility | Docker |
| --- | --- | --- |
| `Example.Domain.Tests` | Pure entity, value-object, invariant, and transition tests | No |
| `Example.Application.Tests` | Services, validators, mappings, DI, repository interactions, and cancellation | No |
| `Example.Infrastructure.IntegrationTests` | EF mappings, repositories, constraints, transactions, migrations, and DI against Testcontainers PostgreSQL | Yes |
| `Example.Api.IntegrationTests` | `WebApplicationFactory<Program>`, HTTP contracts, ProblemDetails, auth providers, OpenAPI, and authenticated PostgreSQL CRUD | Yes for PostgreSQL-backed tests |
| `Example.Architecture.Tests` | Project/assembly dependencies, framework isolation, and layer ownership | No |

Run focused tests while developing, then the full suite:

```bash
dotnet test tests/Example.Domain.Tests/Example.Domain.Tests.csproj
dotnet test tests/Example.Application.Tests/Example.Application.Tests.csproj
dotnet test tests/Example.Infrastructure.IntegrationTests/Example.Infrastructure.IntegrationTests.csproj
dotnet test tests/Example.Api.IntegrationTests/Example.Api.IntegrationTests.csproj
dotnet test tests/Example.Architecture.Tests/Example.Architecture.Tests.csproj
dotnet test
```

Authentication integration tests cover missing and invalid tokens, insufficient permissions, and successful access without relying on live cloud providers. OpenAPI tests verify Development-only exposure and contract freshness. After API contract changes, run:

```bash
sh scripts/generate-openapi.sh
sh scripts/generate-openapi.sh --check
```
