import { readFileSync, statSync } from 'node:fs';
import { extname, join, resolve } from 'node:path';
import { readdirSync } from 'node:fs';

const projectRoot = resolve(process.cwd());
const srcRoot = join(projectRoot, 'src');

const allowedExtensions = new Set(['.ts', '.html', '.scss']);
const ignoredDirs = new Set(['node_modules', 'dist', '.angular', '.git']);

const checks = [
  { pattern: /ReactiveFormsModule/g, message: 'ReactiveFormsModule is forbidden. Use @angular/forms/signals.' },
  { pattern: /\bFormBuilder\b/g, message: 'FormBuilder is forbidden. Use form() from @angular/forms/signals.' },
  { pattern: /\bFormGroup\b/g, message: 'FormGroup is forbidden for new code. Use signal form models.' },
  { pattern: /\bFormControl\b/g, message: 'FormControl is forbidden for new code. Use signal form models.' },
  { pattern: /formControlName\s*=|\[formControlName\]/g, message: 'formControlName is forbidden. Use [formField].' },
  { pattern: /\[formGroup\]/g, message: '[formGroup] is forbidden. Use signal forms.' },
  { pattern: /\[\(ngModel\)\]/g, message: '[(ngModel)] is forbidden. Use signal state + [formField].' },
  { pattern: /provideZoneChangeDetection\s*\(/g, message: 'provideZoneChangeDetection is forbidden. Keep zoneless mode.' },
  { pattern: /from\s+['"]zone\.js(?:\/testing)?['"]/g, message: 'zone.js imports are forbidden in zoneless apps.' }
];

const violations = [];

function walk(dirPath) {
  for (const entry of readdirSync(dirPath, { withFileTypes: true })) {
    if (ignoredDirs.has(entry.name)) {
      continue;
    }

    const fullPath = join(dirPath, entry.name);

    if (entry.isDirectory()) {
      walk(fullPath);
      continue;
    }

    const ext = extname(entry.name);
    if (!allowedExtensions.has(ext)) {
      continue;
    }

    const stats = statSync(fullPath);
    if (!stats.isFile()) {
      continue;
    }

    const content = readFileSync(fullPath, 'utf8');
    const relativePath = fullPath.replace(projectRoot + '\\', '').replaceAll('\\', '/');

    for (const check of checks) {
      const regex = new RegExp(check.pattern.source, check.pattern.flags);
      let match;
      while ((match = regex.exec(content)) !== null) {
        const line = content.slice(0, match.index).split('\n').length;
        violations.push({
          file: relativePath,
          line,
          message: check.message,
          snippet: match[0]
        });
      }
    }
  }
}

walk(srcRoot);

if (violations.length > 0) {
  console.error('Angular standards violations found:');
  for (const violation of violations) {
    console.error(`- ${violation.file}:${violation.line} ${violation.message} [${violation.snippet}]`);
  }
  process.exit(1);
}

console.log('Angular standards check passed.');
