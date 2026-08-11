# Runbook

## 1) Bootstrap

Run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\setup\00_bootstrap.ps1
```

## 2) Configure your agent client

- Preferred: Cline extension in VS Code.
- Keep GitHub Copilot enabled.

Use MCP template:

- `config/mcp/mcp.servers.template.json`

Copy values into your agent MCP settings and adjust paths/secrets.
For VS Code + Cline in this workspace, run:

```powershell
.\scripts\setup\06_sync_cline_mcp.ps1
```

Configured now in workspace MCP:

- filesystem
- git
- browser
- angular-cli (`@angular/cli mcp`)
- dotnet (`roslyn-codelens-mcp`)
- microsoft_learn (`mcp-remote https://learn.microsoft.com/api/mcp`)
- mongodb (`mongodb-mcp-server` in `--readOnly` mode)

Actual runtime files (what is used by tools):

- Workspace MCP: `.vscode/mcp.json`
- Cline MCP: `%APPDATA%\Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`

Note: there is currently no official Optimus UI MCP server package published on npm.

Important:

- `@cyanheads/git-mcp-server` and `mcp-docker-server` are third-party packages.
- Before production use, verify publisher reputation, pin versions, and run in least-privilege mode.
- `roslyn-codelens-mcp` is third-party and should be treated as non-first-party.
- Use Angular's official MCP entry point: `npx @angular/cli mcp`.

Microsoft backend guidance:

- For official Microsoft architectural guidance in-agent, use `@microsoft/learn-cli` (command `mslearn`) for docs/code search flows.
- Microsoft Learn MCP is connected via `mcp-remote` bridge to `https://learn.microsoft.com/api/mcp`.
- Current npm discovery did not show a first-party Microsoft .NET code-context MCP server package equivalent to Roslyn semantic tooling.

## 8) Professional confidence check

Run:

```powershell
.\scripts\setup\05_confidence_check.ps1
```

This validates:

- .NET SDK presence
- MCP package resolution
- Microsoft Learn MCP connectivity
- Sample .NET solution build (if present)
- MongoDB MCP connection-string readiness

## 9) .NET backend quality/security gates

Run:

```powershell
.\scripts\ci\Invoke-DotNetQualityGates.ps1
```

This enforces:

- Restore + build with warnings as errors
- Tests with code coverage output
- Format/analyzer verification (`dotnet format --verify-no-changes`)
- NuGet vulnerability scan (top-level + transitive)
- Secret scanning locally when `gitleaks` exists and always in CI

## 3) Task routing policy

Read:

- `config/routing/routing.policy.yaml`

Local first for routine/private tasks. Escalate to Copilot for complex tasks.
This file is a policy reference and CI-validated artifact; it does not automatically switch models in VS Code/Cline by itself.

## 3.1) Model role policy and actual model selection

`config/models/models.policy.json` defines desired role mapping (coding/reasoning/embeddings) for your local stack.

Important: this policy file is not directly consumed by Cline/Copilot model picker.

- Cline/Ollama model selection is configured in Cline settings/UI.
- Copilot model selection is configured in Copilot chat/model controls.

Use this command to verify policy-vs-installed local model readiness:

```powershell
.\scripts\setup\05_confidence_check.ps1
```

## 4) Security baseline

Read and enforce:

- `config/security/security.policy.yaml`

For local development, prefer project-scoped user secrets (or environment variables) instead of hard-coding database credentials in tracked files.

Example:

```powershell
cd src/Backend/LocalEnterprise.Api
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://<user>:<password>@localhost:27017/admin"
dotnet user-secrets set "MongoDb:DatabaseName" "local-enterprise-dev"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7081"
dotnet user-secrets set "Jwt:Audience" "localenterprise.api"

cd ../LocalEnterprise.Auth
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://<user>:<password>@localhost:27017/admin"
dotnet user-secrets set "MongoDb:DatabaseName" "local-enterprise-dev"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7081"
dotnet user-secrets set "Jwt:Audience" "localenterprise.api"
dotnet user-secrets set "Auth:BootstrapAdmin:Username" "apiadmin"
dotnet user-secrets set "Auth:BootstrapAdmin:Password" "<local dev password>"
dotnet user-secrets set "Auth:BootstrapAdmin:Roles:0" "Admin"
```

`LocalEnterprise.Auth` issues tokens through OpenIddict and the SPA uses Authorization Code + PKCE for local development sign-in.

## 5) Benchmark models

Run:

```powershell
.\scripts\benchmark\Invoke-OllamaBenchmark.ps1 -Model qwen2.5:7b-instruct
```

Optional with multiple models:

```powershell
.\scripts\benchmark\Invoke-OllamaBenchmark.ps1 -Model qwen2.5:7b-instruct,llama3:latest
```

## 6) CI and quality gates

Initialize git before enabling CI templates:

```powershell
git init
```

Then add your project-specific test/lint commands and security scanners.

## 7) Monthly operations

- Re-run model benchmark.
- Review MCP server access list.
- Patch/update models and dependencies.
- Rotate tokens and verify no secrets are in repo.
