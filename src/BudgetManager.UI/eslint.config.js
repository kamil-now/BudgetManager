// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const stylistic = require('@stylistic/eslint-plugin');

module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      angular.configs.tsRecommended,
    ],
    languageOptions: {
      parserOptions: {
        projectService: {
          allowDefaultProject: ['e2e/*.ts', 'playwright.config.ts'],
        },
        tsconfigRootDir: __dirname,
      },
    },
    plugins: {
      '@stylistic': stylistic,
    },
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
      'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
      'no-debugger': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': [
        'warn',
        {
          argsIgnorePattern: '^_+$',
          varsIgnorePattern: '^_+$',
        },
      ],
      'no-empty-pattern': 'off',
      'no-prototype-builtins': 'off',
      'no-new-func': 'warn',
      'no-redeclare': 'warn',
      '@typescript-eslint/return-await': 'warn',
      'no-sparse-arrays': 'warn',
      'no-void': 'warn',
      '@stylistic/linebreak-style': 'off',
      '@stylistic/newline-per-chained-call': ['warn', { ignoreChainWithDepth: 2 }],
      '@stylistic/no-multiple-empty-lines': 'warn',
      '@stylistic/padding-line-between-statements': [
        'warn',
        { blankLine: 'never', prev: 'import', next: 'import' },
      ],
      '@stylistic/quotes': ['warn', 'single'],
      '@stylistic/function-call-spacing': ['warn', 'never'],
      '@stylistic/quote-props': ['warn', 'consistent-as-needed'],
      '@stylistic/space-before-blocks': 'warn',
      '@stylistic/keyword-spacing': ['warn', { before: true, after: true }],
      '@stylistic/space-infix-ops': 'warn',
      '@stylistic/space-unary-ops': 'warn',
      '@stylistic/object-curly-spacing': ['warn', 'always'],
      '@stylistic/comma-spacing': ['warn', { before: false, after: true }],
      '@stylistic/array-bracket-spacing': ['warn', 'never'],
      '@stylistic/arrow-spacing': ['warn', { before: true, after: true }],
      '@stylistic/key-spacing': ['error', { beforeColon: false, afterColon: true }],
      '@stylistic/semi': ['warn', 'always'],
      '@stylistic/indent': ['warn', 2, { SwitchCase: 1 }],
    },
  },
  {
    files: ['**/*.spec.ts', 'e2e/**/*.ts'],
    rules: {
      '@typescript-eslint/no-shadow': ['warn', { ignoreTypeValueShadow: true }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
