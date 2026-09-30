// @ts-check
import js from '@eslint/js';
import angular from 'angular-eslint';
import boundaries from 'eslint-plugin-boundaries';
import prettier from 'eslint-config-prettier';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

/**
 * Layering (Clean Architecture applied to Angular). Per feature:
 * data-access (HTTP, SSE, DTOs, mappers) -> state (store, pure reducer) -> ui (presentational)
 * -> the page that wires state to ui. Features never import each other; shared code lives in
 * core/ or shared/; domain code imports no framework.
 */
const sameFeature = (...types) => ({
  to: {
    element: {
      types: { anyOf: types },
      captured: { feature: '{{ from.element.captured.feature }}' },
    },
  },
});

export default defineConfig([
  {
    ignores: [
      'dist/',
      '.angular/',
      'coverage/',
      'playwright-report/',
      'test-results/',
      'node_modules/',
    ],
  },
  {
    files: ['**/*.ts'],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      angular.configs.tsRecommended,
    ],
    languageOptions: {
      parserOptions: {
        // e2e/ has its own tsconfig.json; the Playwright config sits beside the workspace ones.
        projectService: {
          allowDefaultProject: ['playwright.config.ts'],
          defaultProject: 'e2e/tsconfig.json',
        },
        tsconfigRootDir: import.meta.dirname,
      },
    },
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'ph', style: 'kebab-case' },
      ],
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'ph', style: 'camelCase' },
      ],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-signals': 'error',
      '@angular-eslint/use-lifecycle-interface': 'error',
      '@angular-eslint/no-output-on-prefix': 'error',
      '@typescript-eslint/no-explicit-any': 'error',
      // Angular components, directives and services are decorated classes.
      '@typescript-eslint/no-extraneous-class': ['error', { allowWithDecorator: true }],
      '@typescript-eslint/consistent-type-imports': ['error', { fixStyle: 'inline-type-imports' }],
      '@typescript-eslint/explicit-member-accessibility': ['error', { accessibility: 'no-public' }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/prefer-self-closing-tags': 'error',
    },
  },
  {
    files: ['src/**/*.ts'],
    plugins: { boundaries },
    settings: {
      'import/resolver': { node: { extensions: ['.ts', '.js'] } },
      'boundaries/elements': [
        { type: 'data-access', pattern: 'src/app/features/*/data-access', capture: ['feature'] },
        { type: 'domain', pattern: 'src/app/features/*/domain', capture: ['feature'] },
        { type: 'state', pattern: 'src/app/features/*/state', capture: ['feature'] },
        { type: 'ui', pattern: 'src/app/features/*/ui', capture: ['feature'] },
        { type: 'page', pattern: 'src/app/features/*', capture: ['feature'] },
        { type: 'core', pattern: 'src/app/core' },
        { type: 'shared', pattern: 'src/app/shared' },
        // Folder elements, most specific first: the first match classifies a file.
        { type: 'app', pattern: 'src/app' },
        { type: 'bootstrap', pattern: 'src' },
      ],
    },
    rules: {
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            // Which packages each layer may use is enforced by no-restricted-imports below.
            { allow: { to: { module: { origin: 'external' } } } },
            {
              from: { element: { type: 'bootstrap' } },
              allow: { to: { element: { type: 'app' } } },
            },
            {
              from: { element: { type: 'app' } },
              allow: { to: { element: { types: { anyOf: ['app', 'core', 'shared', 'page'] } } } },
            },
            { from: { element: { type: 'core' } }, allow: { to: { element: { type: 'core' } } } },
            {
              from: { element: { type: 'shared' } },
              allow: { to: { element: { types: { anyOf: ['shared', 'core'] } } } },
            },
            {
              from: { element: { type: 'page' } },
              allow: sameFeature('page', 'ui', 'state', 'domain'),
            },
            {
              from: { element: { type: 'page' } },
              allow: { to: { element: { types: { anyOf: ['shared', 'core'] } } } },
            },
            {
              from: { element: { type: 'state' } },
              allow: sameFeature('state', 'data-access', 'domain'),
            },
            {
              from: { element: { type: 'state' } },
              allow: { to: { element: { types: { anyOf: ['shared', 'core'] } } } },
            },
            {
              from: { element: { type: 'data-access' } },
              allow: sameFeature('data-access', 'domain'),
            },
            {
              from: { element: { type: 'data-access' } },
              allow: { to: { element: { types: { anyOf: ['shared', 'core'] } } } },
            },
            { from: { element: { type: 'ui' } }, allow: sameFeature('ui', 'domain') },
            { from: { element: { type: 'ui' } }, allow: { to: { element: { type: 'shared' } } } },
            { from: { element: { type: 'domain' } }, allow: sameFeature('domain') },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/**/domain/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@angular/*', '@angular/**', 'rxjs', 'rxjs/*'],
              message: 'Domain code is pure TypeScript: no Angular, no RxJS.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/**/ui/**/*.ts', 'src/app/features/*/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: '@angular/common/http',
              message: 'Components never call HttpClient; use a data-access service.',
            },
          ],
        },
      ],
      'no-restricted-globals': [
        'error',
        { name: 'fetch', message: 'Components never call fetch; use a data-access service.' },
      ],
    },
  },
  prettier,
]);
