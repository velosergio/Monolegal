# Quickstart — Validación de Spec 026 (Comentarios de Código y Documentación de Arquitectura)

Guía de validación end-to-end. Confirma que los cuatro entregables (arquitectura en README, comentarios SOLID, mapa DI, ADRs) cumplen los criterios de éxito. No contiene implementación; remite a `contracts/` y `data-model.md`.

## Prerrequisitos

- Repositorio en la rama `026-comentarios-arquitectura`.
- .NET 10 SDK instalado (para verificar que el build sigue verde).
- Visor de Markdown con soporte Mermaid (para los diagramas).

## Escenario 1 — Clean Architecture en README (US1 / SC-001)

1. Abrir `README.md` y `docs/architecture.md`.
2. **Verificar**: existe descripción de cada capa (Domain, Application, Infrastructure, Api), del Worker y del Frontend, con su responsabilidad única.
3. **Verificar**: se enuncia la dirección de dependencias (externas → internas) y que los cambios tecnológicos quedan confinados a Infrastructure.
4. **Verificar**: hay al menos un diagrama (Mermaid) de capas/dependencias.

**Resultado esperado**: una persona sin contexto puede explicar la organización en capas en < 15 min.

## Escenario 2 — Comentarios SOLID en clases clave (US2 / SC-002)

1. Buscar el marcador de la convención en el código:
   ```bash
   grep -rln "SOLID:" backend/ worker/ --include="*.cs"
   ```
2. **Verificar**: aparecen las clases clave (servicios de dominio/aplicación, repositorios, notificadores, validadores, proveedores de email, workers) según `research.md` D2.
3. Abrir una muestra (p. ej. `MongoInvoiceRepository`, `InvoiceTransitionService`) y **verificar** que el comentario nombra el/los principio(s) y da una justificación concreta, conforme a `contracts/convencion-comentarios-solid.md`.
4. Confirmar que el build sigue verde:
   ```bash
   dotnet build backend/Backend.sln   # o el .sln/.csproj correspondiente
   ```

**Resultado esperado**: 100% de las clases clave comentadas; build verde.

## Escenario 3 — Mapa de Inyección de Dependencias (US3 / SC-003)

1. Abrir `docs/dependency-injection.md`.
2. Tomar 3–5 entradas de la tabla y **verificarlas contra el registro real**:
   ```bash
   grep -nE "AddSingleton|AddScoped|AddTransient|AddHostedService" \
     backend/Infrastructure/Configuration/DependencyInjection.cs backend/Api/Program.cs
   ```
3. **Verificar**: cada abstracción registrada aparece en el documento con su implementación y ciclo de vida correctos; no hay entradas obsoletas ni faltantes.
4. **Verificar**: el documento incluye la nota de mantenimiento (documento vivo).

**Resultado esperado**: correspondencia 1:1 entre el mapa documentado y el registro real.

## Escenario 4 — ADRs (US4 / SC-004)

1. Abrir `docs/adr/README.md` (índice) y `docs/adr/0000-plantilla.md` (plantilla).
2. **Verificar**: cada ADR sigue el formato de `contracts/plantilla-adr.md` (Estado, Fecha, Contexto, Decisión, Alternativas, Consecuencias).
3. **Verificar**: las decisiones no obvias vigentes (ver `research.md` D7) están registradas como ADRs.
4. **Verificar** (FR-010): si algún ADR reemplaza a otro, ambos enlazan correctamente y el sustituido está marcado `Reemplazado`.

**Resultado esperado**: 100% de las decisiones no obvias vigentes documentadas con formato consistente.

## Escenario 5 — Idioma y documentos vivos (SC-006 / FR-011 / FR-012)

1. **Verificar**: toda la documentación entregada está en español.
2. **Verificar**: los documentos incluyen la disciplina de "documento vivo" (actualización en el mismo PR que cambia la estructura descrita).

**Resultado esperado**: documentación íntegra en español y con criterio de mantenimiento explícito.
