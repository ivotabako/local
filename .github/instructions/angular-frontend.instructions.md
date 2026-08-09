---
description: "Use when creating or modifying Angular 22 frontend code under src/Frontend/localenterprise-web. Enforces standalone APIs, zoneless Angular, signal-first state, Signal Forms, strict typing, testing, accessibility, and frontend security rules."
applyTo: "src/Frontend/localenterprise-web/**"
---

# Angular Frontend Coding Standards

## Mandatory architecture

- Use standalone APIs only.
- Keep the application zoneless. Do not add `zone.js`, ZoneJS-based providers, or zone-dependent patterns.
- Prefer signal-based state with `signal`, `computed`, and `effect`.
- Keep component templates simple and move non-trivial view logic into TypeScript.
- Prefer feature-oriented structure and small focused components.

## Forms policy

- Use Signal Forms from `@angular/forms/signals` for all new forms.
- Use `form()` plus `[formField]` bindings.
- Define validation rules in the schema callback of `form()`.
- Do not introduce `ReactiveFormsModule`, `FormBuilder`, `FormGroup`, `FormControl`, `formControlName`, `[formGroup]`, or `[(ngModel)]` for new feature work.
- For legacy integrations, helpers from `@angular/forms/signals/compat` are allowed only as temporary migration adapters.

## Component and state rules

- Prefer inputs, outputs, and injected services with explicit types.
- Use `protected` for template-only component members when appropriate.
- Avoid `any`, implicit `unknown` escapes, and unnecessary type assertions.
- Prefer explicit class and style bindings over `ngClass` and `ngStyle` where practical.
- Keep data transformation in computed state or dedicated helpers, not inline templates.

## Testing and quality

- Add or update focused tests for non-trivial component logic, state transitions, validation, guards, interceptors, and security-sensitive flows.
- Keep tests deterministic and avoid time-based or network-dependent behavior without explicit seams.
- Do not suppress template, type, lint, or validation errors to get a build through.
- Frontend changes should pass `npm run quality:all` before merge.

## Security and reliability

- Validate and sanitize user-controlled data before submitting requests.
- Treat data from route params, query params, forms, storage, and backend responses as untrusted until validated.
- Keep auth tokens in memory-only application state unless an explicit requirement mandates persistence.
- Do not bypass Angular sanitization or use raw HTML insertion without a documented, reviewed reason.
- Avoid logging secrets, tokens, PII, or full backend error payloads.
- Prefer strict Content Security Policy compatibility and avoid patterns that require unsafe inline script execution.

## Accessibility and UX

- Use semantic HTML, accessible labels, keyboard support, and visible error states.
- Keep loading, empty, error, and success states explicit.
- Build responsive layouts that work on desktop and mobile.