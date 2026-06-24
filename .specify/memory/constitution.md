# Constitución Monolegal

## Principios Fundamentales

### I. Arquitectura Limpia (NO NEGOCIABLE)

Cada componente sigue estricta separación por capas. Backend emplea capas Domain/Application/Infrastructure/Api con dirección explícita de dependencias (capas externas dependen de internas, nunca al revés). Frontend organiza componentes por feature con límites claros de hooks/estado. Beneficios: Testabilidad, mantenibilidad, infraestructura reemplazable. Cada módulo debe tener una única responsabilidad claramente documentada. Cambios tecnológicos (ej: MongoDB → PostgreSQL, cambio de proveedor email) no deben propagarse más allá de la capa Infrastructure.

### II. Principios SOLID (NO NEGOCIABLE)

- **S**ingular Responsibility: Una razón para cambiar por clase/función
- **O**pen/Closed: Abierto para extensión (interfaces), cerrado para modificación
- **L**iskov Substitution: Tipos derivados sustituyen tipos base sin romper contratos
- **I**nterface Segregation: Muchas interfaces específicas sobre un contrato inflado
- **D**ependency Inversion: Depender de abstracciones (interfaces), inyectar concretamente

El incumplimiento bloquea merge en PR. Code review enfocado en fuga de responsabilidad y acoplamiento. Abstraer implementaciones concretas vía interfaces; inyectar via constructor/propiedad.

### III. Desarrollo Dirigido por Especificaciones (SDD)

Cada feature comienza con especificación escrita en formato GIVEN/WHEN/THEN (BDD). Especificación define criterios de aceptación antes de escribir código. Tests derivan de specs; specs guían diseño. Sin requisitos ambiguos llegan a desarrollo. Specs son documentos vivos—actualizados si descubrimientos de diseño justifican clarificación. Specs deben ser testeables y falsables (sin lenguaje vago como "debe ser rápido").

**DIRECTRIZ OBLIGATORIA**: Toda documentación de especificaciones (specs, plans, tasks, checklists, ADRs, comentarios en código relacionados a requisitos) DEBE estar en **español**. Sin excepción. El código fuente puede tener comentarios en inglés para librerías, pero los requisitos, diseño y decisiones arquitectónicas se documentan en español para accesibilidad del equipo.

### IV. Desarrollo Test-First (NO NEGOCIABLE)

- **Unit Tests**: >85% cobertura código (xUnit backend, Vitest frontend)
- **Integration Tests**: Flujo cross-layer, contratos repositorio, endpoints API
- **E2E Tests**: Jornadas críticas del usuario (listar facturas → filtrar → transicionar → confirmar)
- **CI Gate**: Sin merge sin pasar todas las suites de tests

Ciclo Red-Green-Refactor forzado. Tests escritos primero, feature implementado segundo, luego refactorizado. Reportes de cobertura publicados por PR. Tests inestables disparan investigación inmediata; si no confiable, remover hasta estabilizar.

### V. Frontend de Calidad Producción

Código frontend mantenido con mismo rigor que backend. TypeScript strict mode forzado (sin `any`). Biome linting/formatting 100% compliant—sin excepciones. React Doctor corre en cada PR; cero warnings. Accesibilidad de componentes (WCAG A) verificada en tests. Dark mode debe ser built-in desde día uno (no retrofitted). Presupuestos de performance: TTI < 2s, Lighthouse performance > 90. Diseño responsive testeado en dispositivos/browsers reales (no solo chrome dev tools).

### VI. Código Observable y Mantenible

- **Backend Logging**: Serilog structured logging en formato JSON; cada acción significativa logueada con contexto (userId, facturaId, duración, resultado)
- **Frontend Logging**: Error boundaries y degradación elegante; console warnings solo en desarrollo
- **Documentación**: Diagramas de arquitectura en README; patrones SOLID/DI explicados en comentarios; decision records (ADRs) para decisiones no obvias
- **Inyección de Dependencias**: Constructor injection explícito (sin magic service locators); configuración DI container centralizada y documentada

## Requisitos de Stack Tecnológico

**Backend**: ASP.NET Core 10, Minimal APIs (sin full MVC), MongoDB Driver (sin EF), FluentValidation, Serilog (structured logs)  
**Frontend**: React 19+, Vite (sin webpack), TypeScript strict, componentes shadcn/ui, TanStack Query para server state, Motion para animaciones  
**Testing**: xUnit + Shouldly (backend), Vitest + Testing Library (frontend), Playwright (E2E)  
**Calidad**: Biome (linting/formatting), React Doctor (inspección React), dotnet format (backend)  
**Infraestructura**: Docker Compose con servicios separados (frontend, backend, worker, MongoDB); imágenes optimizadas para VPS producción (<500MB tamaño final); sin secrets embebidos

## Workflow de Desarrollo & Code Review

1. **Fase Especificación**: Escribir specs GIVEN/WHEN/THEN en roadmap/feature spec antes de implementar
2. **Generación de Tareas**: Romper specs en tareas discretas, ordenadas por dependencias
3. **Implementación**: Red-Green-Refactor; tests pasan antes de pushear código feature
4. **Code Review**:
   - ✅ Todos los tests pasando (CI/CD gate)
   - ✅ Cobertura ≥85%, sin drops de cobertura
   - ✅ SOLID/Clean Architecture verificados
   - ✅ Biome + React Doctor compliant (frontend)
   - ✅ Specs atendidas (sin spec creep)
   - ✅ Documentación actualizada (README, ADR si necesario)
5. **Merge & Deploy**: Squash commits con referencia spec (ej: "feat(spec-1.2): Implementar transiciones Invoice"); trigger pipeline deployment

Sin commits a `main` bypassed review. Sin test skips permitidos (`[Ignore]`, `.skip()`). Hotfixes deben aún pasar full test suite.

## Seguridad & Cumplimiento

- **Autenticación**: JWT tokens con rol Admin-only; path autenticación único
- **Validación**: FluentValidation en todos inputs API; validación frontend refleja reglas backend
- **Secrets**: Solo variables de entorno (Docker secrets para producción); sin credenciales hardcodeadas
- **Persistencia Datos**: MongoDB connection pooling configurado; shutdown hooks limpios

## Performance & Escalabilidad

- **API**: Endpoints stateless; queries ≤200ms bajo carga normal
- **Frontend**: Code splitting por route; componentes lazy-loaded; <50KB main bundle gzipped
- **Base Datos**: Índices en campos frecuentemente queridos (`Status`, `ClientId`, `CreatedAt`); paginación forzada (sin queries sin límite)
- **Worker**: Horizontalmente escalable vía Docker replicas; sin estado en-memoria (todo estado en MongoDB)

## Gobernanza

Esta Constitución supersede todas las prácticas informales. Cada PR explícitamente referencia qué principios/specs cumple. Enmiendas requieren:
1. Justificación (¿por qué el principio está poco claro o es dañino?)
2. Lenguaje propuesto (específico, testeable)
3. Evaluación de impacto (¿qué proyectos/equipos afectados?)
4. Acuerdo unánime del equipo antes de adopción

**Versión**: 1.0.0 | **Ratificada**: 2026-06-24 | **Última Enmienda**: 2026-06-24
