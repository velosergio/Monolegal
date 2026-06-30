# Data Model — Spec 026 (entidades documentales)

Esta feature no introduce entidades de persistencia. El "modelo de datos" describe las **entidades documentales** que se producen, sus campos, reglas de validación (derivadas de los FR) y relaciones. Son artefactos versionados en el repositorio, no registros en base de datos.

## Entidad 1 — Documentación de Arquitectura

Representa la explicación de la organización en capas del sistema (README + `docs/architecture.md`).

| Campo | Descripción | Regla / Origen |
|-------|-------------|----------------|
| Descripción por capa | Responsabilidad única de Domain, Application, Infrastructure, Api, Worker, Frontend | FR-001 — todas las capas presentes |
| Dirección de dependencias | Regla explícita: externas → internas, nunca al revés; cambios tecnológicos confinados a Infrastructure | FR-002 |
| Diagrama(s) | Al menos un diagrama (Mermaid) de capas y relaciones de dependencia | FR-003 |

**Validación**: Debe coincidir con la organización real de `backend/` (Domain/Application/Infrastructure/Api), `worker/` y `frontend/`.

## Entidad 2 — Comentario SOLID de Clase

Anotación a nivel de clase en código fuente (XML-doc en C#).

| Campo | Descripción | Regla / Origen |
|-------|-------------|----------------|
| Principio(s) | Uno o más de SRP, OCP, LSP, ISP, DIP | FR-004 — al menos uno declarado |
| Justificación | Frase breve que explica por qué la clase encarna el principio | FR-004 |
| Relación interfaz↔implementación | Cuando aplica, deja explícito DIP/OCP entre abstracción e implementación concreta | FR-005 |

**Relaciones**: 1 comentario ↔ 1 clase clave (ver definición en research D2). Cuando la clase implementa una interfaz, el comentario referencia la abstracción.

**Validación**: El 100% de las clases clave identificadas tienen el comentario (SC-002). El build del backend permanece verde tras añadirlos.

## Entidad 3 — Mapa de Inyección de Dependencias

Tabla documentada (`docs/dependency-injection.md`) sincronizada con el contenedor real.

| Campo | Descripción | Regla / Origen |
|-------|-------------|----------------|
| Abstracción | Interfaz/tipo registrado (p. ej. `IInvoiceRepository`) | FR-006 |
| Implementación | Tipo concreto resuelto (p. ej. `MongoInvoiceRepository`) | FR-006 |
| Ciclo de vida | Singleton / Scoped / Transient | FR-006 |
| Registrado en | Archivo y punto de registro (`DependencyInjection.cs` / `Program.cs`) | FR-007 — centralizado |

**Validación**: Correspondencia 1:1 sin entradas faltantes ni obsoletas frente al registro real (SC-003). Nota de mantenimiento incluida (documento vivo, FR-012).

## Entidad 4 — Architecture Decision Record (ADR)

Documento por decisión arquitectónica (`docs/adr/NNNN-titulo.md`).

| Campo | Descripción | Regla / Origen |
|-------|-------------|----------------|
| Identificador | Número secuencial `NNNN` + título | FR-008 |
| Estado | Propuesto / Aceptado / Reemplazado / Obsoleto | FR-008 / FR-010 |
| Fecha | Fecha de la decisión | FR-008 |
| Spec | Enlace a la spec de origen (cuando aplica) | Convención del repo |
| Contexto | Situación y fuerzas en juego | FR-008 |
| Decisión | Qué se decidió | FR-008 |
| Alternativas consideradas | Opciones evaluadas y por qué se descartaron | FR-008 |
| Consecuencias | Efectos positivos y negativos | FR-008 |
| Reemplaza / Reemplazado por | Enlace al ADR sustituido o sustituto | FR-010 |

**Estados (transiciones)**: `Propuesto → Aceptado → Reemplazado/Obsoleto`. Un ADR reemplazado enlaza al ADR que lo sustituye y viceversa.

**Validación**: El 100% de las decisiones no obvias vigentes están registradas con formato consistente (SC-004).

## Restricción transversal

Todas las entidades documentales se redactan en **español** (FR-011) y se tratan como **documentos vivos**, actualizados en el mismo cambio que altera la estructura que describen (FR-012).
