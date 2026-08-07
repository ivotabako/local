# August 2026 Updated Plan

## What remains valid

- Hybrid workflow: Copilot + local models.
- Ollama as local runtime.
- MCP as standard integration protocol.

## 2026 updates applied

- Copilot treated as agent platform, not only completion.
- MCP governance is first-class (allowlists, scope, auditability).
- Model selection is benchmark-driven, not static by name.
- Local/cloud burst policy included.
- Monthly model lifecycle review included.

## Target architecture

1. Editor layer: VS Code, GitHub Copilot, local coding agent.
2. Runtime layer: Ollama.
3. Model roles: coding, reasoning, embeddings.
4. Tool layer: MCP servers.
5. Governance layer: security policies + CI gates.

## Delivery phases

1. Baseline and policy.
2. Runtime and model pull.
3. Agent integration.
4. MCP setup.
5. Benchmark and tune.
6. CI/security gates.

## Acceptance criteria

- Local and cloud routing policy exists and is followed.
- MCP servers are scoped and validated.
- Benchmark report is generated.
- Security policy and quality-gate templates are committed.
