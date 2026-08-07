# Local AI Workstation (Hybrid: Copilot + Local LLM)

This workspace implements an August 2026 local-first AI development setup.

## Goals

- Use GitHub Copilot for premium cloud tasks.
- Use local models for routine/private/offline tasks.
- Use MCP servers with least-privilege access.
- Enforce security and quality gates by default.

## Quick Start (PowerShell)

1. Review policy files in `config/`.
2. Run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\setup\00_bootstrap.ps1
```

3. Open runbook:

```powershell
code .\docs\RUNBOOK.md
```

## Current machine profile detected

- CPU: Intel i7-12700KF
- RAM: 31.82 GB
- GPU: NVIDIA GeForce RTX 5070
- Ollama: installed
- Existing local models: llama3:latest, qwen2.5:7b-instruct

This machine can support a strong local 7B-14B workflow and selective larger-model usage.
