# Workspace Engineering Baseline

These rules apply to all generated or modified code in this workspace.

## Delivery expectations

- Prefer the smallest safe change that solves the root cause.
- Keep touched code better than you found it: remove duplication, tighten naming, and simplify control flow when it can be done safely in the same change.
- Do not bypass failing tests, analyzers, type checks, or security checks by suppression unless the user explicitly asks and the reason is documented in code.
- When changing existing behavior, add or update tests before or with the implementation change so the behavior is specified and protected.
- Treat warnings as defects to resolve, not noise to ignore.

## Architecture and design

- Follow SOLID, DRY, separation of concerns, explicit abstractions, and dependency inversion.
- Keep business rules out of UI and infrastructure glue code.
- Prefer composition over inheritance unless inheritance is clearly the simpler domain model.
- Avoid speculative abstractions. Introduce interfaces, patterns, and layers when they reduce coupling or improve testability.
- Keep public APIs small, intentional, and backward compatible unless the task explicitly allows breaking changes.

## Testing and verification

- Practice test-driven development for new backend behavior and non-trivial frontend logic: start from a failing test or add the nearest focused test as part of the change.
- Add regression tests for bugs before fixing them when practical.
- Prefer fast, deterministic unit tests. Add integration tests for persistence, auth, messaging, serialization, or other infrastructure seams.
- Do not consider work complete until the relevant local quality gate or the narrowest relevant test/build check has passed.

## Security baseline

- Treat OWASP Top 10 and OWASP ASVS as mandatory review lenses for generated code.
- Use secure defaults: least privilege, deny by default, explicit allowlists, secure transport, and safe error handling.
- Never hard-code secrets, tokens, connection strings, private keys, or credentials in tracked files.
- Validate all untrusted input at boundaries and encode or sanitize data for the target sink.
- Do not log secrets, tokens, raw credentials, or sensitive personal data.
- Prefer vetted framework security features over custom authentication, authorization, crypto, serialization, or input sanitization.
- Check new dependencies and generated code for known security risk patterns before considering the change complete.

## Workspace quality gates

- Backend changes should satisfy `./scripts/ci/Invoke-DotNetQualityGates.ps1` before merge.
- Frontend changes under `src/Frontend/localenterprise-web` should satisfy `npm run quality:all` before merge.
- If a narrower focused test or build exists for the touched area, run it first, then run the broader gate when appropriate.

## Scoped instructions

- Angular 22 frontend rules are defined in `.github/instructions/angular-frontend.instructions.md`.
- .NET backend rules are defined in `.github/instructions/dotnet-backend.instructions.md`.
