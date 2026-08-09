---
description: "Use when creating or modifying .NET backend code under src/Backend. Enforces TDD, SOLID, DRY, Clean Architecture, DDD/CQRS-friendly boundaries, secure-by-default ASP.NET Core practices, and mandatory backend quality gates."
applyTo: "src/Backend/**"
---

# .NET Backend Engineering Standards

## Mandatory workflow

- Use test-driven development for new behavior when practical: begin with a failing test or add the nearest focused test in the same change.
- Add regression tests for bug fixes before or alongside the implementation.
- Prefer unit tests for domain and application logic and integration tests for persistence, HTTP, auth, and serialization seams.
- Backend changes should pass `./scripts/ci/Invoke-DotNetQualityGates.ps1` before merge.

## Architecture and boundaries

- Preserve Clean Architecture boundaries already present in `LocalEnterprise.Api`, `LocalEnterprise.Application`, `LocalEnterprise.Domain`, and `LocalEnterprise.Infrastructure`.
- Keep Domain free of infrastructure concerns.
- Keep Application focused on use cases, orchestration, contracts, validation, and policies.
- Keep Infrastructure responsible for persistence and external system implementations behind application interfaces.
- Keep API as the composition root and transport boundary only; avoid business logic in endpoints, controllers, filters, or middleware.
- Prefer explicit dependency injection and constructor injection. Avoid service location, hidden static state, and ambient mutable singletons.

## Design rules

- Follow SOLID and DRY. Extract duplication only when it improves clarity and reuse.
- Model business behavior in domain types, value objects, aggregates, domain services, and domain events where appropriate.
- Use CQRS-style separation when reads and writes have materially different models or policies, but do not add ceremonial layers without payoff.
- Prefer immutable request/response contracts where practical.
- Keep methods short, deterministic, and side-effect aware.
- Use guard clauses and explicit validation at boundaries.
- Do not expose persistence models directly through API contracts unless the model is explicitly designed for that purpose.

## Data access and integration

- Use repository or persistence abstractions where they protect the application core from storage details.
- Avoid leaking MongoDB, EF Core, HTTP, file system, or vendor-specific concerns into Domain.
- Use parameterized queries and framework-safe data access patterns only.
- Make external calls resilient with timeouts, cancellation tokens, and idempotent retry strategy only where safe.
- Keep serialization explicit and prefer `System.Text.Json` unless a stronger reason exists.

## Security requirements

- Treat OWASP Top 10 and OWASP ASVS as mandatory for backend design and code review.
- Enforce authentication and authorization on every externally reachable endpoint unless the endpoint is intentionally anonymous.
- Apply least privilege to database access, service permissions, and runtime identities.
- Validate all untrusted input at the transport boundary and re-check domain invariants in application or domain layers.
- Prevent injection by using parameterized queries, strict allowlist validation, and safe framework APIs.
- Do not roll your own authentication, authorization, crypto, token validation, password hashing, or secrets handling.
- Keep secrets out of source control; use user secrets for development and managed secret stores or environment-backed configuration for deployed environments.
- Avoid insecure deserialization and unsafe file, process, or network operations with untrusted input.
- Log security-relevant failures with enough context for investigation, but never log secrets, tokens, raw credentials, or sensitive personal data.
- Prefer secure HTTP defaults such as HTTPS, HSTS, safe cookies, and restricted CORS.

## Code quality

- Use async flows correctly end-to-end for I/O work; avoid blocking on tasks.
- Pass `CancellationToken` through application and infrastructure operations that can be canceled.
- Do not swallow exceptions. Translate them into meaningful domain or transport outcomes and preserve diagnostics.
- Keep mappings, validation, and orchestration explicit; avoid magic behavior that hides data movement.
- Favor clear names and straightforward code over clever abstractions.