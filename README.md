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

## Authentication

The API supports Keycloak, AWS Cognito, and Microsoft Entra ID through the same bearer-token
and authorization-policy boundary. Select one provider with `Authentication__Provider`.
The protected example endpoints require `examples.read` for GET requests and
`examples.write` for POST, PUT, and DELETE requests. Permissions can be supplied through
`scope`, `scp`, or `roles` claims.

### Local Keycloak

Keycloak is the default Development provider. Start PostgreSQL and Keycloak:

```bash
docker compose up -d --wait
dotnet run --project src/Example.Api/Example.Api.csproj
```

The imported realm contains local-only credentials. Retrieve a read token:

```bash
ACCESS_TOKEN=$(curl --fail --silent --show-error \
  --request POST http://localhost:8080/realms/example/protocol/openid-connect/token \
  --data grant_type=password \
  --data client_id=example-test \
  --data client_secret=local-test-secret \
  --data username=developer \
  --data 'password=Developer123!' \
  --data scope=examples.read | jq -er '.access_token')
```

Use the returned `access_token`:

```bash
curl --fail --header "Authorization: Bearer $ACCESS_TOKEN" \
  https://localhost:7251/api/examples
```

The realm, client secret, user, and password are safe local-development values and must not
be reused outside the local environment.

### Production Cognito

Set the following values through deployment configuration:

```text
Authentication__Provider=Cognito
Authentication__Cognito__Region=us-east-1
Authentication__Cognito__UserPoolId=us-east-1_...
Authentication__Cognito__ClientId=...
Authentication__Cognito__Audience=https://api.example.com
Authentication__Cognito__ResourceServerIdentifier=example-api
```

`Authority` is derived from the region and user-pool ID when omitted. Cognito tokens must be
access tokens with a matching `client_id`. `Audience` is optional and, when configured,
requires a matching resource-bound `aud` claim. Custom scopes such as
`example-api/examples.read` are normalized to the common policy name.

### LocalStack Cognito

LocalStack is a separate local Cognito environment:

```bash
export LOCALSTACK_AUTH_TOKEN=your-localstack-auth-token
docker compose -f compose.yml -f docker-compose.local.yml up -d --wait
dotnet run --project src/Example.Api/Example.Api.csproj --launch-profile cognito-local
```

The initialization hook creates deterministic pool and client IDs, an `example-api` resource
server, read/write scopes and groups, and a development user. LocalStack HTTP issuer and JWKS
settings are rejected outside Development. Cognito is a licensed LocalStack service, so provide
`LOCALSTACK_AUTH_TOKEN` through your shell or secret store; never commit it.

Retrieve the generated local client secret and request a machine token:

```bash
CLIENT_SECRET=$(docker compose -f compose.yml -f docker-compose.local.yml \
  exec -T localstack awslocal cognito-idp describe-user-pool-client \
  --user-pool-id us-east-1_examplepool \
  --client-id examplelocalclient \
  --query UserPoolClient.ClientSecret --output text)

curl --fail --silent --show-error \
  --user "examplelocalclient:$CLIENT_SECRET" \
  --data grant_type=client_credentials \
  --data scope=example-api/examples.read \
  http://cognito-idp.localhost.localstack.cloud:4566/_aws/cognito-idp/oauth2/token
```

With the API running under the `cognito-local` profile, the same end-to-end check is available as:

```bash
bash scripts/smoke-localstack-cognito.sh
```

### Microsoft Entra ID

Register a single-tenant web API, expose delegated scopes named `examples.read` and
`examples.write`, and optionally define application roles with the same values for
client-credential callers. Configure:

```text
Authentication__Provider=EntraId
Authentication__EntraId__Instance=https://login.microsoftonline.com/
Authentication__EntraId__TenantId=...
Authentication__EntraId__ClientId=...
Authentication__EntraId__Audience=api://...
```

Entra delegated permissions are read from `scp`; application permissions are read from
`roles`. Current-user identification prefers `oid` over `sub`, while email falls back to
`preferred_username` when the optional `email` claim is absent.

## Uninstall

```bash
dotnet new uninstall /path/to/dotnet-ddd-monolith-template
```
