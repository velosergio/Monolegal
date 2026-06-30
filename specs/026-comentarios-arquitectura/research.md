# Research — Comentarios de Código y Documentación de Arquitectura (Spec 026)

Feature de documentación. No hay incógnitas tecnológicas (`NEEDS CLARIFICATION`) en el Technical Context; el trabajo de investigación consiste en decidir **convenciones y enfoques** sobre artefactos ya existentes. Cada decisión sigue el formato Decisión / Justificación / Alternativas consideradas.

## D1 — Estado actual de la documentación (auditoría)

**Decisión**: Partir de lo existente y cerrar huecos, no reescribir.

Hallazgos de la auditoría del repositorio:

| Elemento | Estado actual | Acción |
|----------|---------------|--------|
| Clean Architecture en README | Sección "Arquitectura" con estructura de proyectos | Verificar dirección de dependencias y cross-link a docs |
| `docs/architecture.md` | Existe: tabla de componentes, diagrama Mermaid, capas backend | Ampliar con DI y referencia a comentarios SOLID si falta |
| Comentarios SOLID en clases | **0 clases** con declaración SOLID (grep sin resultados) | **Hueco principal**: añadir comentarios |
| Mapa de DI | No existe documento dedicado; registro real en `DependencyInjection.cs` + `Program.cs` | **Crear** `docs/dependency-injection.md` |
| ADRs | `docs/adr/0001`, `0002` con formato consistente | Formalizar: índice + plantilla + ADRs retroactivos |

**Justificación**: El proyecto ya cumple SDD; reaprovechar evita duplicación y respeta "documentos vivos".

**Alternativas consideradas**: Reescribir toda la documentación desde cero (rechazada: desperdicia trabajo válido y rompe enlaces existentes).

## D2 — Definición de "clase clave" para comentarios SOLID

**Decisión**: Son clases clave las que contienen lógica o contratos relevantes para la arquitectura: servicios de dominio (`Domain/Services`), casos de uso (`Application/Services`), notificaciones y resolvers (`Application/Notifications`), validadores (`Application/Validation`), repositorios (`Infrastructure/Repositories`), proveedores/factory de email (`Infrastructure/Email`) y workers (`Infrastructure/Workers`, proyecto `worker/`). Se **excluyen** DTOs, entidades anémicas, enums, options/settings y endpoints triviales.

**Justificación**: Concentra el valor del comentario donde hay decisiones de diseño SOLID reales; evita ruido en tipos de datos triviales. Coincide con el supuesto declarado en la spec.

**Alternativas consideradas**: Comentar todas las clases (rechazada: ruido, mantenimiento costoso); comentar solo interfaces (rechazada: las implementaciones concretas son donde se evidencia OCP/DIP).

## D3 — Formato del comentario SOLID

**Decisión**: Comentario XML-doc de C# a nivel de clase, en español, con una línea que nombre el/los principio(s) y una justificación breve. Patrón:

```csharp
/// <summary>
/// [Responsabilidad de la clase en una frase].
/// SOLID: SRP — única razón de cambio: [X]. DIP — depende de [IInterfaz], inyectada por constructor.
/// </summary>
```

**Justificación**: XML-doc es el estándar de C#, lo recoge IntelliSense y la generación de docs; mantiene la convención del repo (ya hay `<summary>` en `DependencyInjection.cs`). Nombrar el principio + justificación satisface FR-004/FR-005 y es verificable en code review.

**Alternativas consideradas**: Comentarios `//` libres (rechazada: no estructurados, no aparecen en tooling); atributos custom `[SolidPrinciple]` (rechazada: sobreingeniería, no aporta sobre un comentario).

## D4 — Formato y ubicación del mapa de Inyección de Dependencias

**Decisión**: Documento Markdown `docs/dependency-injection.md` con una tabla por capa: `Abstracción | Implementación | Ciclo de vida | Registrado en`. La única fuente de verdad del registro sigue siendo `DependencyInjection.cs` + `Program.cs`; el documento las refleja y enlaza.

**Justificación**: Cumple FR-006/FR-007 (mapeo documentado + centralizado) sin introducir herramientas de generación. Una tabla es trivial de verificar contra el código en code review (SC-003).

**Alternativas consideradas**: Generación automática del mapa desde el contenedor en tiempo de ejecución (rechazada para esta feature: fuera de alcance, complejidad); diagrama Mermaid de dependencias (se puede añadir como complemento, pero la tabla es la fuente verificable).

## D5 — Sincronización mapa DI ↔ registro real

**Decisión**: La sincronía se verifica en code review mediante un ítem de checklist; el documento incluye una nota de mantenimiento ("actualizar al modificar `DependencyInjection.cs`").

**Justificación**: Evita infraestructura de test frágil; la constitución ya exige documentación actualizada por PR (§Workflow paso 4).

**Alternativas consideradas**: Test automatizado que reflexione sobre el `IServiceCollection` y compare con el doc (rechazada: alto coste/fragilidad para una tabla pequeña; reconsiderable si el mapa crece mucho).

## D6 — Estructura del repositorio de ADRs

**Decisión**: Mantener `docs/adr/NNNN-titulo.md` con el formato vigente (Estado, Fecha, Spec, Contexto, Decisión, Consecuencias) y añadir: (a) `docs/adr/README.md` como índice, (b) `docs/adr/0000-plantilla.md` como plantilla, (c) campo **Alternativas consideradas** y **Reemplaza/Reemplazado por** para FR-008/FR-010.

**Justificación**: Estandariza el formato ligero estilo Nygard ya en uso; el índice y la plantilla bajan la fricción de crear nuevos ADRs y dan consistencia (SC-004).

**Alternativas consideradas**: Adoptar MADR completo (rechazada: más pesado que el formato actual, migración innecesaria); herramienta `adr-tools` (rechazada: dependencia externa para algo que es Markdown).

## D7 — Decisiones no obvias a registrar retroactivamente como ADR

**Decisión**: Catalogar las decisiones arquitectónicas no obvias vigentes detectadas en el código/specs y crear ADRs para las que falten. Candidatas iniciales:

- Repositorios como `Singleton` con MongoDB driver (en lugar de `Scoped`) — decisión de ciclo de vida no obvia.
- Selección de proveedor de email en runtime vía factory + fallback `NoOp` en Dev/CI (spec 017).
- Estrategia de migraciones idempotentes como `IHostedService` al arranque (specs 015/018).
- Worker de transiciones como `BackgroundService` con estado en MongoDB (sin estado en memoria).

La lista definitiva se acota en `/speckit-tasks`; cada ADR enlaza a su spec de origen.

**Justificación**: FR-009 exige registrar decisiones no obvias vigentes; estas no son evidentes leyendo solo el código y ya están "decididas".

**Alternativas consideradas**: Documentar solo decisiones futuras (rechazada: deja sin trazabilidad las decisiones estructurales ya tomadas).

## Resumen

No quedan marcadores `NEEDS CLARIFICATION`. Enfoque: reutilizar y cerrar huecos sobre documentación existente, con el comentariado SOLID de clases clave como mayor esfuerzo nuevo, un documento de mapa DI verificable, y la formalización del repositorio de ADRs.
