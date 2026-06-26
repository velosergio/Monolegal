# Quickstart — Validación de la Vista de Configuración (017)

Guía de validación end-to-end de la feature. No contiene implementación; referencia `data-model.md` y `contracts/` para el detalle. Las tareas concretas las genera `/speckit-tasks`.

## Prerrequisitos

- Stack levantado con Docker Compose (frontend, backend+worker in-process, MongoDB) o ejecución local (`dotnet run` API + `npm run dev` frontend).
- Panel accesible (Admin-only).
- Variables de entorno de email (secretos solo por entorno, D8):
  - SMTP: `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__UseStartTls`.
  - Resend: `Email__Resend__ApiKey`, `Email__Resend__FromDomain`.
  - En Dev/CI sin host SMTP ni API key, el envío usa el emisor `NoOp` (no envía); útil para validar UI/persistencia sin correos reales.

## Comandos de calidad y test (CI gates)

```bash
# Backend
dotnet test                 # xUnit + Shouldly (Domain/Application/Integración)
dotnet format --verify-no-changes

# Frontend
npm run lint                # Biome
npm run test                # Vitest + Testing Library
npm run build               # build de producción (presupuestos)
```

Criterios: cobertura ≥85% backend y frontend; React Doctor 100/100 honesto; Biome sin warnings.

---

## Escenarios de validación (mapeados a User Stories)

### US1 — Seleccionar/configurar/validar/persistir el proveedor (P1)

1. Abrir `/configuracion` → la sección "Proveedor de email" muestra proveedor activo, remitente, nombre y **estado de credencial** (sin secreto).
2. Cambiar el proveedor activo (SMTP↔Resend) → aparecen los campos propios sin perder lo introducido.
3. Editar `fromAddress` con valor inválido y guardar → validación inline, sin petición.
4. Pulsar **"Validar credenciales"** → toast de éxito (credencial aceptada) o error con motivo (rechazada/caída). Sin enviar correo.
5. Guardar configuración válida → toast de éxito; recargar la página → los valores persisten (GET refleja lo guardado).
6. **Runtime**: con un proveedor activo guardado, forzar/observar un ciclo del worker → el envío real usa el proveedor activo (sin reinicio).

**Esperado**: FR-001..FR-008, FR-002a/b; SC-001, SC-002, SC-003, SC-007.

### US2 — Plantillas (P2)

1. Sección "Plantillas": seleccionar "Recordatorio" → se muestran subject/body efectivos y las **variables admitidas**.
2. Insertar una variable admitida → la **vista previa** se actualiza con datos de ejemplo.
3. Introducir `{{factura.inexistente}}` y guardar → rechazo con mensaje de variable no admitida.
4. Editar válido y guardar → toast éxito; recargar → persiste (`isCustomized: true`).
5. "Restablecer por defecto" → confirmación → vuelve al contenido base (`isCustomized: false`).
6. Verificar que un envío real/prueba usa la plantilla guardada.

**Esperado**: FR-009..FR-015; SC-004.

### US3 — Prueba de envío (P2)

1. Sección "Prueba de envío": introducir destino inválido → validación impide envío.
2. Introducir destino válido + plantilla → "Enviar prueba" → toast de éxito confirmando destino (o error con motivo si falla el proveedor).
3. Botón en estado de carga durante el envío; no permite doble envío.

**Esperado**: FR-016..FR-019; SC-005.

### US4 — Herramientas globales (P3)

1. Preparar datos: facturas con `LastNotificationOutcome == Failed` y alguna en estado notificable con `None`.
2. **Reenvío manual** → toast con `{ attempted, resent, failed }`; las `Failed` exitosas pasan a `Sent`.
3. **Limpieza/saneamiento** → **confirmación obligatoria** → toast con `{ sanitized }`; las `None` notificables pasan a `Failed`.
4. Ejecutar herramientas sin candidatos → mensaje "nada que procesar" (no error).
5. Componer: `sanitize` luego `resend-failed` recupera los atascados.

**Esperado**: FR-020..FR-023; SC-006.

### Transversal — Accesibilidad, responsive, calidad

- Toda la vista operable solo con teclado, foco visible; diálogo de confirmación con foco atrapado y `Esc`.
- Información por color con etiqueta textual (estado credencial, resultados).
- "Reducir movimiento" activo → animaciones atenuadas sin perder información.
- Sin desbordamiento horizontal en móvil/escritorio.
- React Doctor 100/100 honesto; presupuestos de perf (TTI < 2s).

**Esperado**: FR-024..FR-027; SC-008, SC-009, SC-010.

---

## Verificación de seguridad de secretos

- En ninguna respuesta de API (`GET /email`, validate, etc.) aparece `password` ni `apiKey`.
- Los logs de validación/prueba/herramientas no contienen el secreto.
- La UI nunca renderiza el valor de la credencial, solo su estado.

---

## Notas de integración

- El worker de transiciones corre **in-process con la API**; `SettingsBackedEmailService` lee `SystemSettings.Email` por envío (cambio de proveedor en runtime).
- Defaults de plantilla = textos actuales de `EmailTemplateProvider` (fallback cuando no hay personalización).
- Índice sobre `Invoices.LastNotificationOutcome` para las herramientas globales.
