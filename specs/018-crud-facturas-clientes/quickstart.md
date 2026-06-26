# Quickstart — Validación de CRUD de Facturas y Clientes

Guía para validar end-to-end que la funcionalidad cumple la spec. Referencia: [spec.md](./spec.md), [contracts/](./contracts/), [data-model.md](./data-model.md).

## Prerrequisitos

- MongoDB en ejecución (Docker Compose del proyecto).
- Backend (`backend/Api`) y frontend (`frontend`) levantados.
- Entorno `Development` (el seeder puebla 3 clientes + facturas con items/dueDate — research D11).

## Comandos

```bash
# Backend
dotnet test backend                      # toda la suite (dominio, contrato, integración)
dotnet run --project backend/Api         # API en https://localhost:xxxx (Swagger en /swagger)

# Frontend
cd frontend
pnpm install
pnpm test                                # Vitest
pnpm dev                                 # SPA
pnpm exec biome check .                  # lint/format (0 issues)
pnpm playwright test                     # E2E (jornadas críticas)
```

## Escenarios de validación (mapeados a la spec)

### Facturas (HU1)

1. **Crear factura** — En `/facturas`, abrir el formulario, seleccionar un cliente, añadir ≥1 item (descripción, cantidad, precio); verificar que el **total se calcula solo** (read-only), fijar vencimiento y confirmar.
   - Esperado: `201`, toast de éxito, la factura aparece en la tabla y el dashboard se actualiza sin recargar (RF-001, RF-007, RF-008; CE-001).
2. **Validación de creación** — Confirmar sin cliente / sin items / con cantidad o precio ≤0 / sin vencimiento.
   - Esperado: envío bloqueado, mensajes por campo (RF-002; CE-003).
3. **Cliente inexistente** — Forzar creación con `clientId` inválido (vía API).
   - Esperado: `400`, sin crear (caso límite de concurrencia).
4. **Editar factura no terminal** — Editar items/cliente/vencimiento de una factura `pending`.
   - Esperado: `200`, total recalculado, toast, tabla + dashboard refrescados (RF-003).
5. **Bloqueo en estado terminal** — Intentar editar una factura `pagado`/`desactivado`.
   - Esperado: edición impedida; API `409`/`400` con motivo (RF-004a; AC HU1-6).
6. **Eliminar factura** — Pulsar eliminar → aparece modal de confirmación → confirmar.
   - Esperado: `204`, toast, desaparece de tabla y dashboard; eliminación permanente; permitida en cualquier estado (RF-005, RF-010; CE-007).
7. **Error de backend** — Simular fallo (p. ej. desconectar API).
   - Esperado: toast de error, formulario conserva datos (RF-009; CE-002).

### Clientes (HU2)

1. **Listado + búsqueda** — Entrar a `/clientes`; ver listado paginado; escribir un término.
   - Esperado: filtra por nombre/email, paginación se ajusta, resultado <1s (RF-012, RF-013; CE-005).
2. **Estado vacío** — Buscar un término sin coincidencias.
   - Esperado: estado vacío explícito (caso límite).
3. **Crear cliente** — Formulario con nombre + email (obligatorios) y teléfono/dirección (opcionales).
   - Esperado: `201`, toast, listado se refresca (RF-014, RF-020, RF-021).
4. **Validación + unicidad** — Email con formato inválido; luego email ya existente.
   - Esperado: rechazo con mensaje por campo; duplicado bloqueado (RF-015, RF-015a).
5. **Editar cliente** — Cambiar datos y confirmar.
   - Esperado: `200`, toast, listado actualizado (RF-016).
6. **Eliminar sin facturas** — Confirmar borrado de un cliente sin facturas.
   - Esperado: `204`, toast, desaparece (RF-017).
7. **Eliminar con facturas** — Intentar borrar un cliente con facturas asociadas (p. ej. `seed-cliente-a`).
   - Esperado: `409`, mensaje explicativo, NO se elimina (RF-018; CE-004).

## Migración (research D6)

- Tras el arranque, verificar que las facturas previas (sin items) tienen un item sintético `{ "Concepto", 1, Amount }` y `DueDate` poblada, y que `Amount == Σ subtotales`.
- Reejecutar el arranque: la migración no vuelve a modificar documentos ya migrados (idempotencia).

## Criterios de aceptación de salida

- Suite backend y frontend en verde; cobertura ≥85% (constitución IV).
- Biome y React Doctor sin warnings (constitución V).
- Todos los escenarios anteriores con el resultado esperado.
