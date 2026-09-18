import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import globals from 'globals'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist/**', 'node_modules/**', 'coverage/**'] },

  js.configs.recommended,
  // Type-aware rules catch what a purely syntactic linter cannot: floating promises,
  // unsafe any, misused async handlers.
  ...tseslint.configs.recommendedTypeChecked,
  ...pluginVue.configs['flat/recommended'],

  {
    languageOptions: {
      globals: { ...globals.browser },
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
        extraFileExtensions: ['.vue'],
      },
    },
    rules: {
      // -- Whitespace and attribute wrapping are a formatter's job. Leaving them on
      //    means 200+ warnings that say nothing about correctness.
      'vue/max-attributes-per-line': 'off',
      'vue/singleline-html-element-content-newline': 'off',
      'vue/html-self-closing': 'off',
      'vue/html-indent': 'off',
      'vue/attributes-order': 'off',

      // -- Correctness.
      // An unawaited promise in a handler swallows its rejection, which is exactly how
      // a failed save becomes silence. Require an explicit `void` to opt out.
      '@typescript-eslint/no-floating-promises': 'error',
      '@typescript-eslint/no-misused-promises': [
        'error',
        { checksVoidReturn: { attributes: false } },
      ],
      '@typescript-eslint/consistent-type-imports': [
        'error',
        { prefer: 'type-imports', fixStyle: 'inline-type-imports' },
      ],
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      'vue/multi-word-component-names': 'off',
      'vue/component-api-style': ['error', ['script-setup']],
      'vue/define-macros-order': ['error', { order: ['defineProps', 'defineEmits'] }],
      'vue/no-unused-refs': 'error',
      'vue/require-default-prop': 'off',
      eqeqeq: ['error', 'smart'],
      'no-console': ['warn', { allow: ['warn', 'error'] }],
    },
  },

  // Vue SFCs: vue-eslint-parser handles the template, TypeScript the <script> block.
  {
    files: ['**/*.vue', 'src/main.ts'],
    languageOptions: {
      parserOptions: { parser: tseslint.parser },
    },
    rules: {
      // typescript-eslint cannot resolve `.vue` modules, so every component import
      // reads as `any` and the no-unsafe-* family fires on correct code. `vue-tsc`
      // type-checks these files properly, so nothing is actually going unchecked.
      '@typescript-eslint/no-unsafe-argument': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
    },
  },

  // Test files.
  {
    files: ['**/*.spec.ts'],
    languageOptions: { globals: { ...globals.node } },
    rules: {
      // Fires on `vi.mocked(service.method)`. The services are plain object literals
      // with no `this` usage, so there is no unbound-method hazard to catch here.
      '@typescript-eslint/unbound-method': 'off',
      // Test doubles legitimately return loosely typed fixtures.
      '@typescript-eslint/no-unsafe-assignment': 'off',
    },
  },

  // Config and setup files run in Node and sit outside the app's TS program.
  {
    files: ['*.config.ts', 'vitest.setup.ts'],
    languageOptions: { globals: { ...globals.node } },
    ...tseslint.configs.disableTypeChecked,
  },
)
