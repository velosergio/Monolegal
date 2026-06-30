#!/usr/bin/env node
// E2E contra el stack Docker Compose (backend en :5000, ver docker-compose.override.yml).
// `test:e2e` por defecto apunta a :5155 (`dotnet run` local).

import { spawnSync } from 'node:child_process'
import path from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const frontendRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const playwrightCli = path.join(frontendRoot, 'node_modules', '@playwright', 'test', 'cli.js')

const dockerApi = 'http://127.0.0.1:5000'

const result = spawnSync(process.execPath, [playwrightCli, 'test', ...process.argv.slice(2)], {
  stdio: 'inherit',
  cwd: frontendRoot,
  env: {
    ...process.env,
    E2E_API_URL: dockerApi,
    VITE_API_PROXY_TARGET: dockerApi,
  },
})

process.exit(result.status ?? 1)
