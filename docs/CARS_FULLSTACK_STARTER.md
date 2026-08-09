# Cars Full-Stack Starter

This workspace now includes a full baseline for:

- Angular 22 zoneless frontend with Optimus UI CRUD grid
- .NET 10 minimal API (`LocalEnterprise.Api`) with JWT authorization
- .NET 10 OpenIddict-based auth/token issuer (`LocalEnterprise.Auth`)
- MongoDB persistence in infrastructure layer
- Unit + integration tests (including authorization guard test)

## Architecture

- Domain: `LocalEnterprise.Domain` (`Car` aggregate)
- Application: `LocalEnterprise.Application` (`ICarRepository`, `CarService`)
- Infrastructure: `LocalEnterprise.Infrastructure` (`MongoCarRepository`)
- API host: `LocalEnterprise.Api` (secured `/api/cars` endpoints)
- Auth host: `LocalEnterprise.Auth` (OAuth 2.0/OpenID Connect token endpoint at `POST /connect/token`)

## Run order (development)

1. Start auth server:

```powershell
dotnet run --project src/Backend/LocalEnterprise.Auth/LocalEnterprise.Auth.csproj
```

2. Start API server:

```powershell
dotnet run --project src/Backend/LocalEnterprise.Api/LocalEnterprise.Api.csproj
```

3. Start frontend:

```powershell
cd src/Frontend/localenterprise-web
npm run start
```

## Required config

### API user-secrets (recommended)

```powershell
cd src/Backend/LocalEnterprise.Api
dotnet user-secrets init
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://<user>:<password>@localhost:27017/admin"
dotnet user-secrets set "MongoDb:DatabaseName" "local-enterprise-dev"
```

### Token server alignment

Both API and Auth must share the same values:

- `Jwt:Issuer` = `https://localhost:7081`
- `Jwt:Audience` = `localenterprise.api`

OpenIddict manages token signing keys inside the auth host. The API validates issued tokens using the auth server's issuer metadata.

### Local auth user

Configure a local development user in user secrets for `LocalEnterprise.Auth` using a hashed password.
See `docs/RUNBOOK.md` for the exact commands.

## Auth note

`LocalEnterprise.Auth` now uses OpenIddict for standards-based token issuance. The current frontend continues to use the OAuth password grant for local development compatibility, but the recommended long-term browser flow is authorization code + PKCE.

## Tests

```powershell
dotnet test src/Backend/LocalEnterprise.slnx
```

Frontend test:

```powershell
cd src/Frontend/localenterprise-web
npm test -- --watch=false
```
