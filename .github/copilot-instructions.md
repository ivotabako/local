# Angular Frontend Coding Standards (Angular 22, Greenfield)

These rules apply to all code under `src/Frontend/localenterprise-web`.

## Mandatory architecture

- Use standalone APIs only.
- Use signal-based state (`signal`, `computed`, `effect`) for component state.
- Use Signal Forms from `@angular/forms/signals` for all new forms.
- Keep the application zoneless. Do not add ZoneJS dependencies or Zone-based providers.

## Forms policy

- Use `form()` plus `[formField]` bindings.
- Define validation rules in the schema callback of `form()`.
- Do not introduce `ReactiveFormsModule`, `FormBuilder`, `FormGroup`, `FormControl`, or `ngModel` for new feature work.
- For legacy integrations, migration helpers from `@angular/forms/signals/compat` are allowed only as temporary adapters.

## Change detection and rendering

- Prefer signal-driven updates over manual change detection.
- Keep template logic simple; move non-trivial logic to `computed` values or methods.
- Prefer explicit class/style bindings over `ngClass` and `ngStyle` where possible.

## Quality gates

- All new frontend changes should pass build and test checks.
- Run `npm run quality:all` before merging frontend changes.
- Do not suppress type errors or validation errors to pass checks.

## Security and reliability

- Validate and sanitize user-facing data before submit requests.
- Keep auth tokens in memory-only app state unless a requirement explicitly needs persistence.
- Avoid logging secrets or sensitive payload values.
