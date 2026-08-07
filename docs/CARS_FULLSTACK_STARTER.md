# Cars Full-Stack Starter

This workspace now includes a full baseline for:

- Angular 22 zoneless frontend with PrimeNG CRUD grid
- .NET 10 minimal API (`LocalEnterprise.Api`) with JWT authorization
- .NET 10 auth/token issuer (`LocalEnterprise.Auth`)
- MongoDB persistence in infrastructure layer
- Unit + integration tests (including authorization guard test)

## Architecture

- Domain: `LocalEnterprise.Domain` (`Car` aggregate)
- Application: `LocalEnterprise.Application` (`ICarRepository`, `CarService`)
- Infrastructure: `LocalEnterprise.Infrastructure` (`MongoCarRepository`)
- API host: `LocalEnterprise.Api` (secured `/api/cars` endpoints)
- Auth host: `LocalEnterprise.Auth` (`POST /connect/token`)

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

### JWT alignment

Both API and Auth must share the same values:

- `Jwt:Issuer` = `https://localhost:7081`
- `Jwt:Audience` = `localenterprise.api`
- `Jwt:SigningKey` = same secret in both projects

## Default development credentials

Configured in `LocalEnterprise.Auth/appsettings.Development.json`:

- Username: `apiadmin`
- Password: `ChangeMe_OnlyForLocalDev`

Change these for your environment.

## Tests

```powershell
dotnet test src/Backend/LocalEnterprise.slnx
```

Frontend test:

```powershell
cd src/Frontend/localenterprise-web
npm test -- --watch=false
```
