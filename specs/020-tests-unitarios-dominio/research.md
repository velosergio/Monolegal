# Research — Tests Unitarios del Dominio (spec 020)

Fase 0. Resuelve las decisiones técnicas antes del diseño. No quedan marcadores NEEDS CLARIFICATION.

## Línea base de cobertura (evidencia medida)

Ejecución `dotnet test Monolegal.Domain.Tests --collect:"XPlat Code Coverage"` el 2026-06-29:
**105 pruebas, 0 fallos, 0 omitidas, 183 ms.** Cobertura del proyecto de dominio:

- **Línea base: 62.06% líneas / 58.69% ramas** (105 pruebas).
- **Resultado final (tras spec 020): 100% líneas / 100% ramas** (140 pruebas). Todo el código de dominio escrito a mano queda cubierto. El código autogenerado del `[GeneratedRegex]` de `EmailTemplateRenderer` y los helpers de test (`OverrideCreatedAt`, ejercitado por `Application.Tests`) se excluyen vía `coverlet.runsettings` (`ExcludeByAttribute=GeneratedCodeAttribute,CompilerGeneratedAttribute,ExcludeFromCodeCoverageAttribute`) y `[ExcludeFromCodeCoverage]`. Umbral del 85% superado y forzado por `backend/Tests/Monolegal.Domain.Tests/verify-coverage.ps1`.

Por clase (line-rate):

| Clase | Cobertura | Lectura |
|-------|-----------|---------|
| `EmailTemplateRenderer` + regex generado | 0% | **Hueco principal**: sin ninguna prueba |
| `EmailTemplateVariables` | 0% | **Hueco principal**: sin ninguna prueba |
| `SmtpSettings` | 50% | bordes de propiedades sin tocar |
| `SystemSettings` | 80.9% | cerca del umbral; faltan ramas (`ResetTemplate` miss, etc.) |
| `Invoice` | 95.1% | ✅ ya supera el umbral |
| `InvoiceTransitionService` | 98.4% | ✅ ya supera el umbral |
| `Client`, `StatusChange`, `InvoiceItem`, `InvoiceTransitionsConfig`, `EmailSettings`, `ResendSettings`, `EmailTemplate` | 100% | ✅ |

**Decisión**: El núcleo que pide la spec (US1 transiciones, US2 creación de facturas) ya está cubierto. El déficit hacia el 85% **global** se concentra en `Domain/Email`. El plan ataca ese hueco en lugar de inflar pruebas del núcleo.

- **Rationale**: La métrica de gobernanza (Principio IV) se mide sobre el proyecto de dominio completo; `Domain/Email` pertenece a esa capa y arrastra el promedio.
- **Alternativas consideradas**: (a) Excluir `Domain/Email` de la medición — rechazada: es código de dominio real y comprobable, excluirlo enmascara deuda. (b) Subir cobertura del núcleo al 100% — rechazada: no mueve el global de forma significativa y produce pruebas redundantes.

## Decisión 1 — Cobertura de `Domain/Email` (renderer + catálogo)

- **Decisión**: Añadir `Email/EmailTemplateRendererTests.cs` y `Email/EmailTemplateVariablesTests.cs`.
- **Rationale**: Son funciones puras (sustitución de `{{var}}`, validación de catálogo cerrado) ideales para pruebas deterministas; pasan de 0% a ~100% con pocos casos y son las de mayor palanca sobre el global.
- **Casos clave**: marcador admitido sustituido por su valor; marcador admitido con dato ausente → cadena vacía; marcador NO admitido se deja intacto; plantilla nula/vacía; `ExtractVariables`/`FindInvalidVariables`; `IsAllowed` true/false y `All`/`AllowedSet` consistentes.
- **Alternativas**: probar el renderer indirectamente vía endpoints (Application/Api) — rechazada: rompe el aislamiento de la prueba de dominio y no garantiza cobertura de las ramas del renderer.

## Decisión 2 — Bordes de `SystemSettings` / `SmtpSettings`

- **Decisión**: Ampliar `SystemSettingsEmailTests.cs` con casos de `UpdateEmailSettings(null)` (lanza), `ResetTemplate` sobre tipo inexistente (no cambia `UpdatedAt`) y lectura/asignación de `SmtpSettings`/`ResendSettings`.
- **Rationale**: Pequeño esfuerzo que cierra las ramas restantes (80.9% → ~100%, 50% → 100%).
- **Alternativas**: dejarlas sin cubrir confiando en el margen del núcleo — rechazada: deja la métrica frágil ante futuros cambios.

## Decisión 3 — Formalizar transiciones prohibidas y validación (spec US1/US2)

- **Decisión**: Auditar `InvoiceStatusTransitionsTests`/`InvoiceManualTransitionTests` contra la matriz y asegurar que cada estado de origen tiene al menos un caso de transición **prohibida** que afirma rechazo explícito y estado inalterado; idem para los tres invariantes de creación (cliente, monto, items).
- **Rationale**: SC-002/SC-003 exigen cobertura del 100% de orígenes y de invariantes; la cobertura de líneas no lo garantiza por sí sola. Se cierra el hueco con `[Theory]` parametrizado por la matriz.
- **Matriz de referencia** (de `InvoiceTransitionService.ApplyManualTransition`): `Pending→{PrimerRecordatorio,Pagado}`; `PrimerRecordatorio→{SegundoRecordatorio,Pagado}`; `SegundoRecordatorio→{Desactivado,Pagado}`; `Desactivado→{Pagado}`; `Pagado→{}`.

## Decisión 4 — Determinismo temporal

- **Decisión**: Mantener la inyección de `now` (`InvoiceTransitionService.TryApplyTransition(invoice, config, now)`) y el helper interno `OverrideLastStatusTransitionAt`/`OverrideCreatedAt` (vía `InternalsVisibleTo`) para pruebas de tiempo.
- **Rationale**: Evita pruebas inestables (FR-008); ya es el patrón vigente.
- **Alternativas**: abstraer un `IClock` — rechazada: sobreingeniería para el dominio, que ya recibe el tiempo como parámetro.

## Decisión 5 — Gate automático de cobertura ≥ 85%

- **Decisión**: Hacer fallar la build/CI si la cobertura de líneas del dominio cae < 85%, vía umbral de coverlet. Opción elegida: propiedades MSBuild en una invocación de CI (`/p:CollectCoverage=true /p:Threshold=85 /p:ThresholdType=line /p:ThresholdStat=total`) usando el paquete `coverlet.msbuild`, **o** un paso de verificación que parsee el `coverage.cobertura.xml` (`line-rate ≥ 0.85`). Se decide en tasks según el pipeline; ambas cumplen FR-009/FR-010.
- **Rationale**: Convierte SC-001 en gate ejecutable y reproducible por PR (Principio IV: "reportes de cobertura publicados por PR").
- **Alternativas**: confiar en revisión manual — rechazada: no reproducible, viola el espíritu del CI Gate.

## Decisión 6 — Limpieza de andamiaje

- **Decisión**: Eliminar `UnitTest1.cs` (plantilla vacía generada por `dotnet new xunit`).
- **Rationale**: Ruido sin valor; FR-012 prohíbe pruebas vacías/omitidas.

## Riesgos y notas

- El catálogo de `EmailTemplateVariables` es cerrado (spec 017); las pruebas deben afirmar el conjunto exacto para detectar adiciones/eliminaciones accidentales.
- La cobertura del regex generado (`GeneratedRegex`) cuenta como clase aparte (0% hoy); se cubre indirectamente al ejercitar `Render`/`ExtractVariables`.
- No se espera tocar código de producción; cualquier rama imposible de cubrir se documenta como hallazgo (posible código muerto) en lugar de forzar pruebas artificiales.
