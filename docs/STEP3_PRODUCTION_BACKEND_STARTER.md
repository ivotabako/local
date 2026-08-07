# Step 3: Production Backend Starter

This starter introduces a backend-first structure that aligns with Microsoft .NET practices and your architecture goals.

## Structure

- `src/Backend/LocalEnterprise.Api`: HTTP/API host.
- `src/Backend/LocalEnterprise.Application`: use cases and interfaces.
- `src/Backend/LocalEnterprise.Domain`: core domain model.
- `src/Backend/LocalEnterprise.Infrastructure`: MongoDB and external implementations.
- `src/Backend/LocalEnterprise.Tests.Unit`: fast unit tests.
- `src/Backend/LocalEnterprise.Tests.Integration`: integration-level test seam.

## Design intent

- Domain does not depend on infrastructure.
- Application depends on domain abstractions.
- Infrastructure implements application contracts.
- API composes dependencies and hosts endpoints.

## MongoDB

- Options section: `MongoDb` in appsettings.
- Infrastructure registers `IMongoClient`, `IMongoDatabase`, and `IOrderRepository`.

## CI gates

- `scripts/ci/Invoke-DotNetQualityGates.ps1` now defaults to `src/Backend/LocalEnterprise.slnx`.
- Enforces restore/build/test/format/vulnerability checks.
