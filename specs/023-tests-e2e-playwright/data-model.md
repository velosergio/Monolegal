# Data Model — Tests E2E con Playwright

Esta feature **no introduce entidades de dominio nuevas**; consume el modelo existente como caja negra. Este documento describe (a) los datos sembrados que sirven de precondición, (b) la máquina de estados de factura relevante para los flujos E2E y (c) las entidades de prueba (fixtures/abstracciones) que la suite define.

## 1. Datos sembrados (precondición conocida)

Fuente de verdad: `backend/Application/Seeding/SeedDataDefinition.cs` (idempotente; se re-aplica con el endpoint de flush). Montos en COP.

### Clientes (3)

| Id estable | Nombre | Email |
|---|---|---|
| `seed-cliente-a` | Cliente A | cliente.a@monolegal.test |
| `seed-cliente-b` | Cliente B | cliente.b@monolegal.test |
| `seed-cliente-c` | Cliente C | cliente.c@monolegal.test |

### Facturas (8)

| Cliente | Monto (COP) | Estado inicial (API) | Etiqueta visible |
|---|---|---|---|
| A | 1.500.000 | `pending` | Pendiente |
| A | 3.200.000 | `primerrecordatorio` | 1er Recordatorio |
| A | 900.000 | `pagado` | Pagado (terminal) |
| B | 5.400.000 | `segundorecordatorio` | 2do Recordatorio |
| B | 750.000 | `desactivado` | Desactivado (terminal) |
| C | 2.100.000 | `pending` | Pendiente |
| C | 4.100.000 | `primerrecordatorio` | 1er Recordatorio |
| C | 12.000.000 | `segundorecordatorio` | 2do Recordatorio |

**Distribución inicial por estado**: Pendiente=2, 1er Recordatorio=2, 2do Recordatorio=2, Pagado=1, Desactivado=1 (total=8).

> Nota: las pruebas eligen su factura objetivo **por estado/cliente**, no por posición fija, para tolerar evoluciones del seed (FR-008).

## 2. Máquina de estados de factura (relevante para US2/US3)

Fuente de verdad: `backend/Domain/Services/InvoiceTransitionService.GetAllowedTransitions`. El frontend consume `allowedTransitions` del backend; las pruebas no replican la matriz, la descubren desde la UI.

```text
Pending ──────────▶ PrimerRecordatorio ─────────▶ SegundoRecordatorio ─────────▶ Desactivado
   │                      │                              │                            │
   └──────────────────────┴───────────┬──────────────────┴────────────────────────────┘
                                       ▼
                                    Pagado   (destino desde cualquier estado activo)

Estados terminales (sin transiciones de salida): Pagado, Desactivado.
```

| Estado origen | Destinos permitidos |
|---|---|
| Pendiente | 1er Recordatorio, Pagado |
| 1er Recordatorio | 2do Recordatorio, Pagado |
| 2do Recordatorio | Desactivado, Pagado |
| Desactivado | Pagado |
| Pagado | — (terminal) |

**Implicación para E2E**:
- Transición no terminal verificable sin volver terminal: p. ej. **Pendiente → 1er Recordatorio** (delta limpio en dashboard, no cierra el ciclo). Buena elección para US2/US3.
- Estado **terminal sin control de transición**: usar la factura `pagado` (Cliente A) — verifica US2 escenario 3.

## 3. Entidades de prueba (fixtures y abstracciones de la suite)

No son entidades de dominio; son construcciones de la capa de pruebas (`frontend/e2e/`).

- **Fixture de reset de datos** (`reset-data.ts`): expone una operación `resetData()` que invoca `POST /api/settings/maintenance/flush-database` y espera la respuesta `{ seeded, clientsCreated, invoicesCreated }`. Garantiza la precondición conocida antes de las pruebas que mutan estado.
- **Fixture base de Playwright** (`test.ts`): extiende `test` de `@playwright/test` inyectando `resetData` y, opcionalmente, los page objects. Permite que cada spec declare su necesidad de estado limpio.
- **Page object / helpers de localización** (opcional, `pages/`): encapsulan los localizadores accesibles estables:
  - Lista de facturas: filtro `aria-label="Filtrar por estado"`, filas de tabla, badge de estado, botón "Ver detalle…".
  - Detalle/transición: select `aria-label="Nuevo estado"`, botón "Cambiar Estado", toast de confirmación.
  - Dashboard: tarjeta "Total de facturas", gráfico "Facturas por estado" (`aria-label="Distribución de facturas por estado"`).
- **Lectura de distribución del dashboard**: helper que lee los conteos visibles del dashboard para comparar **deltas** (origen −1, destino +1) en US3, evitando valores absolutos frágiles.

## 4. Reglas de validación reflejadas en las pruebas

- El filtro por estado devuelve únicamente facturas del estado seleccionado (FR-003).
- El control de transición ofrece **solo** los destinos permitidos por el backend para el estado actual (FR-004, US2.1).
- Un estado terminal no ofrece control de transición y comunica que no admite cambios (FR-005, US2.3).
- Tras una transición, lista y dashboard reflejan el nuevo estado de forma consistente y persistente (FR-006, SC-006).
