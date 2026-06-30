# Feature Specification: Comentarios de Código y Documentación de Arquitectura

**Feature Branch**: `026-comentarios-arquitectura`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "Spec 6.2: Code Comments & Architecture Doc — GIVEN código implementado WHEN se revisa THEN: Clean Architecture explicada en README; SOLID principles aplicados (comentarios en clase); Dependency Injection claramente mapeado; Decision records (ADR) para cambios importantes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender la Clean Architecture desde el README (Priority: P1)

Un desarrollador nuevo (o un revisor de código) abre el proyecto y necesita comprender, sin leer todo el código fuente, cómo está organizado el sistema en capas, hacia dónde fluyen las dependencias y qué responsabilidad tiene cada capa (Domain, Application, Infrastructure, Api, Worker, Frontend).

**Why this priority**: Es el punto de entrada de cualquier persona al proyecto. Sin una explicación clara de la arquitectura, el resto de la documentación (SOLID, DI, ADR) carece de contexto. Entrega valor inmediato y es independientemente verificable.

**Independent Test**: Se valida abriendo el README y confirmando que contiene una sección de arquitectura con descripción de cada capa, la regla de dirección de dependencias (capas externas dependen de internas) y al menos un diagrama. Una persona ajena al equipo debe poder explicar el flujo de dependencias tras leerla.

**Acceptance Scenarios**:

1. **Given** un desarrollador nuevo sin contexto previo, **When** lee la sección de arquitectura del README, **Then** identifica las capas del backend (Domain, Application, Infrastructure, Api), el Worker y el Frontend, y la responsabilidad única de cada una.
2. **Given** la sección de arquitectura, **When** el lector consulta el diagrama de capas, **Then** la dirección de dependencias es explícita (las capas externas dependen de las internas, nunca al revés) y coincide con la organización real del código.
3. **Given** un cambio tecnológico hipotético (ej. cambiar el proveedor de email o la base de datos), **When** el lector consulta la documentación, **Then** entiende que el impacto queda confinado a la capa Infrastructure.

---

### User Story 2 - Verificar principios SOLID con comentarios en clase (Priority: P2)

Un revisor de código revisa las clases clave del backend y necesita confirmar que aplican los principios SOLID, apoyándose en comentarios a nivel de clase que declaren explícitamente qué principio encarna cada componente y por qué.

**Why this priority**: Refuerza la conformidad SOLID (no negociable según la constitución) y acelera el code review, pero depende de que primero exista la explicación de arquitectura para dar contexto.

**Independent Test**: Se valida inspeccionando un conjunto representativo de clases clave (servicios de aplicación, repositorios, validadores, servicios de email) y confirmando que cada una tiene un comentario que nombra el/los principio(s) SOLID aplicado(s) y su justificación.

**Acceptance Scenarios**:

1. **Given** una clase clave del backend (ej. un servicio de aplicación o un repositorio), **When** el revisor lee su comentario de clase, **Then** encuentra declarado qué principio SOLID encarna y una breve justificación.
2. **Given** una interfaz y su implementación concreta, **When** el revisor revisa los comentarios, **Then** queda claro cómo se aplican Inversión de Dependencias y Abierto/Cerrado.
3. **Given** un componente que no aplica algún principio relevante de forma evidente, **When** el revisor lo inspecciona, **Then** el comentario lo aclara para evitar ambigüedad.

---

### User Story 3 - Mapear la Inyección de Dependencias (Priority: P2)

Un desarrollador necesita saber qué abstracción (interfaz) se resuelve a qué implementación concreta y con qué ciclo de vida, sin tener que rastrear manualmente el registro del contenedor DI a lo largo del código.

**Why this priority**: La constitución exige que la configuración DI esté centralizada y documentada. Un mapa de DI claro reduce errores de configuración y facilita reemplazar implementaciones. Depende del contexto de arquitectura (P1).

**Independent Test**: Se valida consultando el mapa de DI documentado y confirmando que cada interfaz registrada aparece con su implementación concreta y su ciclo de vida, y que el mapa coincide con el registro real del contenedor.

**Acceptance Scenarios**:

1. **Given** el documento/sección de mapeo de DI, **When** el desarrollador busca una interfaz registrada, **Then** encuentra su implementación concreta y su ciclo de vida (singleton, scoped, transient).
2. **Given** el registro centralizado del contenedor DI, **When** se compara con el mapa documentado, **Then** ambos coinciden (sin entradas faltantes ni obsoletas).
3. **Given** la documentación de DI, **When** un desarrollador quiere sustituir una implementación, **Then** identifica el único punto de registro a modificar.

---

### User Story 4 - Registrar decisiones importantes con ADRs (Priority: P3)

Cuando se toma una decisión arquitectónica no obvia (elección de tecnología, patrón estructural, compromiso técnico), el equipo necesita un registro persistente que capture el contexto, las alternativas consideradas y las consecuencias, para que decisiones futuras no repitan el análisis ni contradigan acuerdos previos.

**Why this priority**: Aporta memoria institucional y trazabilidad, pero es el elemento menos bloqueante para entender el sistema hoy; puede crecer incrementalmente.

**Independent Test**: Se valida confirmando que existe un repositorio de ADRs con un formato consistente y que las decisiones arquitectónicas no obvias ya tomadas en el proyecto están documentadas como ADRs.

**Acceptance Scenarios**:

1. **Given** una decisión arquitectónica no obvia ya tomada en el proyecto, **When** se consulta el repositorio de ADRs, **Then** existe un ADR que documenta contexto, decisión, alternativas y consecuencias.
2. **Given** un ADR, **When** un miembro del equipo lo lee, **Then** comprende por qué se tomó la decisión sin necesidad de consultar a quien la tomó.
3. **Given** una decisión que se revierte o supera, **When** se registra el cambio, **Then** el ADR anterior queda marcado como reemplazado/obsoleto enlazando al ADR que lo sustituye.

---

### Edge Cases

- ¿Qué ocurre cuando el código evoluciona y la documentación de arquitectura, los comentarios SOLID o el mapa de DI quedan desactualizados? La documentación debe tratarse como documento vivo y actualizarse en el mismo PR que cambia la estructura.
- ¿Qué pasa si una clase clave carece de comentario SOLID? Debe detectarse en code review y bloquear el merge.
- ¿Cómo se evita que el mapa de DI diverja del registro real del contenedor? Debe existir un criterio de verificación que detecte divergencias.
- ¿Qué sucede con decisiones tomadas antes de adoptar los ADRs? Se documentan retroactivamente al menos las no obvias y vigentes.
- ¿Cómo se maneja una decisión que contradice un ADR existente? Se crea un nuevo ADR que marca el anterior como reemplazado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El README DEBE incluir una sección de arquitectura que describa cada capa del backend (Domain, Application, Infrastructure, Api), el Worker y el Frontend, con su responsabilidad única.
- **FR-002**: La documentación de arquitectura DEBE explicar explícitamente la regla de dirección de dependencias (capas externas dependen de internas, nunca al revés) e indicar que los cambios tecnológicos quedan confinados a la capa Infrastructure.
- **FR-003**: La documentación de arquitectura DEBE incluir al menos un diagrama de las capas y sus relaciones de dependencia.
- **FR-004**: Las clases clave del backend DEBEN incluir un comentario a nivel de clase que declare el/los principio(s) SOLID que aplican y una breve justificación.
- **FR-005**: Los comentarios SOLID DEBEN dejar explícita la relación entre interfaces y sus implementaciones concretas respecto a Inversión de Dependencias y Abierto/Cerrado.
- **FR-006**: El proyecto DEBE proporcionar un mapeo documentado de Inyección de Dependencias que liste, para cada abstracción registrada, su implementación concreta y su ciclo de vida.
- **FR-007**: El mapeo de DI documentado DEBE corresponder con el registro real del contenedor (sin entradas faltantes ni obsoletas) y la configuración DI DEBE estar centralizada.
- **FR-008**: El proyecto DEBE mantener un repositorio de Architecture Decision Records (ADR) con un formato consistente que capture, por decisión: contexto, decisión, alternativas consideradas, estado y consecuencias.
- **FR-009**: Toda decisión arquitectónica no obvia ya tomada y vigente DEBE estar registrada como ADR.
- **FR-010**: Cuando una decisión reemplaza a otra, el ADR previo DEBE marcarse como reemplazado/obsoleto enlazando al ADR que lo sustituye.
- **FR-011**: Toda la documentación de esta feature (README, comentarios de requisitos/arquitectura, mapa de DI, ADRs) DEBE estar redactada en español, conforme a la directriz de la constitución.
- **FR-012**: La documentación de arquitectura, los comentarios SOLID, el mapa de DI y los ADRs DEBEN tratarse como documentos vivos y actualizarse en el mismo cambio que altera la estructura que describen.

### Key Entities *(include if feature involves data)*

- **Documentación de Arquitectura**: Sección del README que representa la organización en capas del sistema; atributos: descripción por capa, regla de dirección de dependencias, diagrama(s).
- **Comentario SOLID de Clase**: Anotación asociada a una clase clave; atributos: principio(s) aplicado(s), justificación; relación con la clase y, cuando aplica, con su interfaz.
- **Mapa de Inyección de Dependencias**: Registro que relaciona cada abstracción con su implementación concreta y ciclo de vida; debe permanecer sincronizado con el contenedor DI real.
- **Architecture Decision Record (ADR)**: Documento de una decisión arquitectónica; atributos: identificador, título, estado (propuesto/aceptado/reemplazado), contexto, decisión, alternativas, consecuencias, enlaces a ADRs relacionados.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un desarrollador sin contexto previo puede explicar la organización en capas y la dirección de dependencias del sistema tras leer la sección de arquitectura del README en menos de 15 minutos.
- **SC-002**: El 100% de las clases clave del backend identificadas tienen un comentario que declara el/los principio(s) SOLID aplicado(s) y su justificación.
- **SC-003**: El 100% de las abstracciones registradas en el contenedor DI aparecen en el mapa de DI documentado con su implementación concreta y ciclo de vida, sin divergencias respecto al registro real.
- **SC-004**: El 100% de las decisiones arquitectónicas no obvias y vigentes del proyecto están documentadas como ADRs con formato consistente.
- **SC-005**: Un revisor puede verificar, solo con la documentación, que un cambio tecnológico (ej. proveedor de email o base de datos) afecta únicamente a la capa Infrastructure, en una sola sesión de revisión.
- **SC-006**: Toda la documentación entregada por esta feature está en español, sin excepciones.

## Assumptions

- La definición de "clase clave" abarca los componentes con lógica o contratos relevantes para la arquitectura: servicios de aplicación, repositorios, validadores, servicios de notificación/email y puntos de composición; los DTOs y modelos de datos triviales quedan fuera del requisito de comentario SOLID.
- El mapeo de DI se documenta como artefacto de documentación (sección o documento dedicado) y no requiere herramienta de generación automática para esta feature; la sincronización con el contenedor se verifica en code review.
- Los ADRs se almacenan en una ubicación versionada del repositorio con un formato ligero y consistente (contexto, decisión, alternativas, estado, consecuencias).
- La documentación reutiliza y consolida los artefactos ya existentes en `docs/` y el README del proyecto, sin reemplazar la documentación de API generada (feature 025).
- El alcance se limita a documentación, comentarios y registros de decisiones; no incluye refactorizaciones de código más allá de añadir comentarios.
- El público objetivo principal es el equipo de desarrollo y revisores técnicos del proyecto.
