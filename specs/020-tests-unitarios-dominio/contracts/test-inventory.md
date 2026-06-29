# Contrato — Inventario de casos de prueba del dominio (spec 020)

Fase 1. "Contrato" de esta feature de pruebas: el inventario de casos que la suite DEBE contener, expresado en Given/When/Then y trazado a los FR/SC de la spec. Cada caso usa aserciones Shouldly (FR-007). ✅ = ya existe (línea base); ➕ = a añadir/reforzar.

## C1 — Transiciones válidas (FR-001, SC-002) — `InvoiceStatusTransitionsTests` / `InvoiceManualTransitionTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C1.1 | Factura `Pending` | transición manual a `PrimerRecordatorio` / `Pagado` | aplica; estado y `StatusHistory` actualizados | ✅ |
| C1.2 | Factura `PrimerRecordatorio` | transición a `SegundoRecordatorio` / `Pagado` | aplica | ✅ |
| C1.3 | Factura `SegundoRecordatorio` | transición a `Desactivado` / `Pagado` | aplica | ✅ |
| C1.4 | Factura `Desactivado` | transición a `Pagado` | aplica | ✅ |
| C1.5 | `[Theory]` por toda la matriz de destinos permitidos | aplicar transición | resultado exitoso para cada par origen→destino permitido | ➕ (consolidar en Theory) |

## C2 — Transiciones prohibidas (FR-002, SC-002) — `InvoiceManualTransitionTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C2.1 | Cada estado de origen | transición a un destino NO permitido por la matriz | lanza `InvalidOperationException`; `Status` sin cambios | ➕ `[Theory]` cubriendo ≥1 prohibida por origen |
| C2.2 | Factura `Pagado` | cualquier transición / `ApplyPayment` | lanza `InvalidOperationException` | ✅/➕ |

## C3 — Transición automática por tiempo (FR-003) — `InvoiceStatusTransitionsTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C3.1 | Factura con plazo NO cumplido | `TryApplyTransition(now)` | devuelve `false`; estado sin cambios | ✅ |
| C3.2 | Factura con plazo cumplido | `TryApplyTransition(now)` | devuelve `true`; transiciona al siguiente estado | ✅ |
| C3.3 | Factura `Pagado`/`Desactivado` | `TryApplyTransition(now)` | devuelve `false` | ✅ |
| C3.4 | `invoice` o `config` nulos | `TryApplyTransition` | lanza `ArgumentNullException` | ➕ |

## C4 — Creación de facturas válida (FR-004) — `Entities/InvoiceTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C4.1 | Datos válidos (cliente, items, vencimiento) | `Invoice.Create(...)` | estado `Pending`; `Amount == Σ Subtotal`; `StatusHistory` vacío | ✅ |
| C4.2 | Constructor de compatibilidad con monto válido | `new Invoice(clientId, amount)` | item sintético; vencimiento +30d | ✅ |

## C5 — Validaciones de creación (FR-005, SC-003) — `Entities/InvoiceTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C5.1 | `clientId` vacío/blanco | crear | lanza `ArgumentException` | ✅ |
| C5.2 | `amount ≤ 0` | crear | lanza `ArgumentException` | ✅ |
| C5.3 | `items` vacía/nula | `Create` | lanza `ArgumentException` | ✅ |

## C6 — Edición / estado terminal (FR-006) — `Entities/InvoiceTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C6.1 | Factura terminal (`Pagado`/`Desactivado`) | `UpdateDetails(...)` | lanza `InvalidOperationException` | ✅ |
| C6.2 | Factura no terminal | `UpdateDetails(...)` | actualiza campos; recalcula `Amount` | ✅ |

## C7 — InvoiceItem (FR-004/005) — `Entities/InvoiceItemsTests`

| # | Given | When | Then | Estado |
|---|-------|------|------|--------|
| C7.1 | Descripción vacía / cantidad ≤0 / precio ≤0 | construir | lanza `ArgumentException` | ✅ |
| C7.2 | Datos válidos | construir | `Subtotal == Quantity × UnitPrice` | ✅ |

## C8 — EmailTemplateVariables (hueco 0% → cobertura) — `Email/EmailTemplateVariablesTests` ➕

| # | Given | When | Then |
|---|-------|------|------|
| C8.1 | Catálogo | leer `All` | contiene exactamente las 9 variables esperadas, en orden |
| C8.2 | Nombre admitido (`cliente.nombre`) | `IsAllowed` | `true` |
| C8.3 | Nombre no admitido (`foo.bar`) | `IsAllowed` | `false` |
| C8.4 | `All` vs `AllowedSet` | comparar | mismos elementos |

## C9 — EmailTemplateRenderer (hueco 0% → cobertura) — `Email/EmailTemplateRendererTests` ➕

| # | Given | When | Then |
|---|-------|------|------|
| C9.1 | Plantilla con `{{ cliente.nombre }}` y valor presente | `Render` | sustituye por el valor |
| C9.2 | Marcador admitido sin valor en el diccionario | `Render` | sustituye por cadena vacía |
| C9.3 | Marcador NO admitido `{{ foo }}` | `Render` | se deja intacto (`{{ foo }}`) |
| C9.4 | Plantilla `null` / vacía | `Render` | devuelve `""` |
| C9.5 | Espacios variables `{{  factura.id  }}` | `Render` | reconoce y sustituye |
| C9.6 | Plantilla con varios marcadores repetidos | `ExtractVariables` | conjunto sin duplicados |
| C9.7 | Plantilla con admitidos y no admitidos | `FindInvalidVariables` | sólo los no admitidos |
| C9.8 | Plantilla 100% válida | `FindInvalidVariables` | colección vacía |

## C10 — SystemSettings / Smtp bordes (hueco 80.9%/50%) — `SystemSettingsEmailTests` ➕

| # | Given | When | Then |
|---|-------|------|------|
| C10.1 | `UpdateEmailSettings(null)` | invocar | lanza `ArgumentNullException` |
| C10.2 | `ResetTemplate` sobre tipo inexistente | invocar | no cambia `UpdatedAt` |
| C10.3 | `ResetTemplate` sobre tipo existente | invocar | elimina plantilla y refresca `UpdatedAt` |
| C10.4 | `SmtpSettings`/`ResendSettings` | asignar/leer propiedades | valores conservados |

## Gate de cobertura (FR-009, FR-010, SC-001)

- La ejecución con `--collect:"XPlat Code Coverage"` genera `coverage.cobertura.xml`.
- Verificación: `line-rate` del proyecto de dominio ≥ `0.85`. Falla el gate si es menor.
- Higiene (FR-011, FR-012, SC-004, SC-005): suite < 10 s, 0 fallos, 0 omitidas; eliminar `UnitTest1.cs`.
