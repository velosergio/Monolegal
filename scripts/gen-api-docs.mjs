#!/usr/bin/env node
// Generador de documentación de API (Spec 6.1 / feature 025).
//
// Lee el snapshot OpenAPI versionado (docs/openapi.json) y emite, de forma
// determinista y sin dependencias externas:
//   - docs/api-reference.md                         (referencia de endpoints)
//   - docs/postman/monolegal.postman_collection.json (colección Postman v2.1)
//
// Uso:
//   node scripts/gen-api-docs.mjs            genera/actualiza los artefactos
//   node scripts/gen-api-docs.mjs --verify   falla (exit≠0) si la salida difiere
//
// El snapshot se refresca aparte, con el backend en Development:
//   curl http://localhost:5155/openapi/v1.json -o docs/openapi.json
//
// Contrato: specs/025-documentacion-api/contracts/api-docs-generator.md

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const OPENAPI_PATH = join(ROOT, 'docs', 'openapi.json')
const API_REF_PATH = join(ROOT, 'docs', 'api-reference.md')
const POSTMAN_PATH = join(ROOT, 'docs', 'postman', 'monolegal.postman_collection.json')

// ID estable de la colección para que la salida sea reproducible byte a byte.
const POSTMAN_COLLECTION_ID = 'a0b1c2d3-0250-4dcf-9a25-d0c2ed025025'

const METHOD_ORDER = ['get', 'post', 'put', 'patch', 'delete']

/** Resuelve un `$ref` local (#/components/schemas/...) contra el documento. */
function resolveRef(doc, ref) {
  if (!ref || !ref.startsWith('#/')) return null
  return ref
    .slice(2)
    .split('/')
    .reduce((acc, key) => (acc ? acc[key] : undefined), doc)
}

/** Tipo "primario" no nulo de un schema cuyo `type` puede ser un arreglo. */
function primaryType(schema) {
  const t = schema?.type
  if (Array.isArray(t)) return t.find((x) => x !== 'null') ?? t[0]
  return t
}

/**
 * Genera un valor de ejemplo determinista para un schema, resolviendo `$ref`.
 * `seen` evita recursión infinita en esquemas auto-referenciados.
 */
function exampleFor(doc, schema, seen = new Set()) {
  if (!schema) return null
  if (schema.$ref) {
    if (seen.has(schema.$ref)) return null
    const next = new Set(seen)
    next.add(schema.$ref)
    return exampleFor(doc, resolveRef(doc, schema.$ref), next)
  }
  if (Array.isArray(schema.enum) && schema.enum.length > 0) return schema.enum[0]

  const type = primaryType(schema)
  switch (type) {
    case 'object': {
      const out = {}
      const props = schema.properties ?? {}
      for (const key of Object.keys(props)) out[key] = exampleFor(doc, props[key], seen)
      return out
    }
    case 'array':
      return [exampleFor(doc, schema.items, seen)]
    case 'boolean':
      return true
    case 'integer':
      return 0
    case 'number':
      return 0
    case 'string':
      if (schema.format === 'date-time') return '2026-01-01T00:00:00Z'
      if (schema.format === 'uuid') return '00000000000000000000000000000000'
      return 'string'
    default:
      return null
  }
}

/** Nombre legible del schema de un media object (para la tabla de cuerpo). */
function schemaName(schema) {
  if (!schema) return ''
  if (schema.$ref) return schema.$ref.split('/').pop()
  if (primaryType(schema) === 'array' && schema.items?.$ref)
    return `${schema.items.$ref.split('/').pop()}[]`
  return primaryType(schema) ?? ''
}

/** Agrupa las operaciones del documento por tag, ordenadas de forma estable. */
function groupByTag(doc) {
  const groups = new Map()
  for (const path of Object.keys(doc.paths).sort()) {
    const item = doc.paths[path]
    for (const method of METHOD_ORDER) {
      const op = item[method]
      if (!op) continue
      const tag = (op.tags && op.tags[0]) || 'General'
      if (!groups.has(tag)) groups.set(tag, [])
      groups.get(tag).push({ path, method, op })
    }
  }
  return new Map([...groups.entries()].sort(([a], [b]) => a.localeCompare(b)))
}

function anchor(tag) {
  return tag.toLowerCase().replace(/[^a-z0-9]+/g, '-')
}

// --------------------------------------------------------------------------
// Referencia en Markdown
// --------------------------------------------------------------------------
function buildApiReference(doc) {
  const groups = groupByTag(doc)
  const lines = []
  lines.push('# Referencia de la API — Monolegal')
  lines.push('')
  lines.push(
    '> ⚙️ **Archivo generado** por `scripts/gen-api-docs.mjs` desde `docs/openapi.json`. ' +
      'No editar a mano: ejecuta `npm run docs:api` tras refrescar el snapshot.'
  )
  lines.push('')
  lines.push(`**Versión del documento**: ${doc.info?.version ?? 'v1'}`)
  lines.push('')
  const sec = doc.components?.securitySchemes?.Bearer
  if (sec) {
    lines.push(
      `**Autenticación**: ${sec.scheme === 'bearer' ? 'Bearer/JWT' : sec.type} — ` +
        'enviar la cabecera `Authorization: Bearer <token>` en los endpoints protegidos.'
    )
    lines.push('')
  }
  lines.push('## Índice')
  for (const tag of groups.keys()) lines.push(`- [${tag}](#${anchor(tag)})`)
  lines.push('')

  for (const [tag, ops] of groups) {
    lines.push(`## ${tag}`)
    lines.push('')
    for (const { path, method, op } of ops) {
      lines.push(`### \`${method.toUpperCase()} ${path}\``)
      lines.push('')
      if (op.summary) lines.push(`**${op.summary}**`)
      if (op.description) {
        lines.push('')
        lines.push(op.description)
      }
      lines.push('')

      const params = op.parameters ?? []
      if (params.length > 0) {
        lines.push('**Parámetros**')
        lines.push('')
        lines.push('| Nombre | En | Requerido | Tipo |')
        lines.push('|--------|----|-----------|------|')
        for (const p of params) {
          const t = p.schema ? primaryType(p.schema) ?? '' : ''
          lines.push(`| \`${p.name}\` | ${p.in} | ${p.required ? 'sí' : 'no'} | ${t} |`)
        }
        lines.push('')
      }

      const reqSchema = op.requestBody?.content?.['application/json']?.schema
      if (reqSchema) {
        lines.push(`**Cuerpo de la petición** (\`application/json\`): \`${schemaName(reqSchema)}\``)
        lines.push('')
        lines.push('```json')
        lines.push(JSON.stringify(exampleFor(doc, reqSchema), null, 2))
        lines.push('```')
        lines.push('')
      }

      const responses = op.responses ?? {}
      if (Object.keys(responses).length > 0) {
        lines.push('**Respuestas**')
        lines.push('')
        lines.push('| Código | Descripción | Cuerpo |')
        lines.push('|--------|-------------|--------|')
        for (const code of Object.keys(responses).sort()) {
          const r = responses[code]
          const body =
            schemaName(
              r.content?.['application/json']?.schema ??
                r.content?.['application/problem+json']?.schema
            ) || '—'
          lines.push(`| ${code} | ${r.description ?? ''} | ${body} |`)
        }
        lines.push('')
      }
      lines.push('---')
      lines.push('')
    }
  }
  return `${lines.join('\n').trimEnd()}\n`
}

// --------------------------------------------------------------------------
// Colección de Postman v2.1
// --------------------------------------------------------------------------
function buildPostmanUrl(path, op) {
  const segments = path.split('/').filter(Boolean).map((s) => s.replace(/^\{(.+)\}$/, ':$1'))
  const query = (op.parameters ?? [])
    .filter((p) => p.in === 'query')
    .map((p) => ({ key: p.name, value: '' }))
  const variable = (op.parameters ?? [])
    .filter((p) => p.in === 'path')
    .map((p) => ({ key: p.name, value: '' }))

  const queryStr = query.length > 0 ? `?${query.map((q) => `${q.key}=`).join('&')}` : ''
  const url = {
    raw: `{{baseUrl}}/${segments.join('/')}${queryStr}`,
    host: ['{{baseUrl}}'],
    path: segments,
  }
  if (query.length > 0) url.query = query
  if (variable.length > 0) url.variable = variable
  return url
}

function buildPostman(doc) {
  const groups = groupByTag(doc)
  const items = []
  for (const [tag, ops] of groups) {
    const folder = { name: tag, item: [] }
    for (const { path, method, op } of ops) {
      const request = {
        method: method.toUpperCase(),
        header: [],
        url: buildPostmanUrl(path, op),
      }
      if (op.description || op.summary) request.description = op.description ?? op.summary
      const reqSchema = op.requestBody?.content?.['application/json']?.schema
      if (reqSchema) {
        request.header.push({ key: 'Content-Type', value: 'application/json' })
        request.body = {
          mode: 'raw',
          raw: JSON.stringify(exampleFor(doc, reqSchema), null, 2),
          options: { raw: { language: 'json' } },
        }
      }
      folder.item.push({ name: op.summary || `${method.toUpperCase()} ${path}`, request })
    }
    items.push(folder)
  }

  return {
    info: {
      _postman_id: POSTMAN_COLLECTION_ID,
      name: doc.info?.title ?? 'Monolegal API',
      description:
        'Colección generada automáticamente desde docs/openapi.json por scripts/gen-api-docs.mjs. ' +
        'Configura las variables {{baseUrl}} y {{token}} en un entorno (ver monolegal.postman_environment.json).',
      schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
    },
    auth: {
      type: 'bearer',
      bearer: [{ key: 'token', value: '{{token}}', type: 'string' }],
    },
    variable: [{ key: 'baseUrl', value: 'http://localhost:5155', type: 'string' }],
    item: items,
  }
}

// --------------------------------------------------------------------------
// Orquestación
// --------------------------------------------------------------------------
function main() {
  const verify = process.argv.includes('--verify')

  let doc
  try {
    doc = JSON.parse(readFileSync(OPENAPI_PATH, 'utf8'))
  } catch (err) {
    console.error(`No se pudo leer ${OPENAPI_PATH}: ${err.message}`)
    console.error('Refresca el snapshot con el backend en Development:')
    console.error('  curl http://localhost:5155/openapi/v1.json -o docs/openapi.json')
    process.exit(1)
  }

  const apiRef = buildApiReference(doc)
  const postman = `${JSON.stringify(buildPostman(doc), null, 2)}\n`

  const outputs = [
    { path: API_REF_PATH, content: apiRef, label: 'docs/api-reference.md' },
    { path: POSTMAN_PATH, content: postman, label: 'docs/postman/monolegal.postman_collection.json' },
  ]

  if (verify) {
    let drift = false
    for (const { path, content, label } of outputs) {
      let current = ''
      try {
        current = readFileSync(path, 'utf8')
      } catch {
        current = ''
      }
      if (current !== content) {
        drift = true
        console.error(`✗ Desincronizado: ${label} (ejecuta 'npm run docs:api')`)
      } else {
        console.log(`✓ ${label}`)
      }
    }
    if (drift) {
      console.error('La documentación generada está desincronizada con docs/openapi.json.')
      process.exit(1)
    }
    console.log('Documentación generada sincronizada con el snapshot OpenAPI.')
    return
  }

  for (const { path, content, label } of outputs) {
    mkdirSync(dirname(path), { recursive: true })
    writeFileSync(path, content)
    console.log(`Generado ${label}`)
  }
}

main()
