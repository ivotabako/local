# Angular 22 Frontend Standards

This project uses strict Angular 22 standards for greenfield development.

## Baseline

- Angular 22.x
- Standalone components and providers
- Zoneless change detection
- Signal-first state management
- Signal Forms for all new forms

## Rules

1. Use `@angular/forms/signals` APIs (`form`, `FormField`, schema validators) for form modeling and validation.
2. Do not create new code with Reactive Forms APIs (`ReactiveFormsModule`, `FormBuilder`, `FormGroup`, `FormControl`) unless bridging legacy code during migration.
3. Keep `zone.js` out of runtime and tests unless a specific legacy requirement demands it.
4. Treat `provideZonelessChangeDetection()` as required application bootstrap behavior.
5. Prefer `signal`, `computed`, and `effect` as the default component state model.
6. Keep templates declarative and simple; move branching or heavy logic into TypeScript.
7. Keep validation messages explicit and schema-driven.
8. Keep component APIs `protected` where only template-visible members are needed.

## Quality workflow

- Run these checks in frontend changes:
  - `npm run quality:standards`
  - `npm run build`
  - `npm run test`
- CI should fail if forbidden form or zone patterns are introduced.

## Forbidden patterns (new code)

- `ReactiveFormsModule`
- `FormBuilder`
- `FormGroup`
- `FormControl`
- `formControlName`
- `[formGroup]`
- `[(ngModel)]`
- `provideZoneChangeDetection`
- `zone.js` imports

## Preferred references

- Signal Forms overview: https://angular.dev/guide/forms/signals/overview
- Signal Forms essentials: https://angular.dev/essentials/signal-forms
- Signal Forms migration: https://angular.dev/guide/forms/signals/migration
- Zoneless guide: https://angular.dev/guide/zoneless
- Angular style guide: https://angular.dev/style-guide
