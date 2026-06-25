# Especificación de Funcionalidad: Seed Data - 3 Clientes Mínimo

**Rama de Funcionalidad**: `008-seed-data-clientes`

**Creado**: 2026-06-25

**Estado**: Activo

**Entrada**: User description: "### Spec 1.4: Seed Data - 3 Clientes Mínimo — GIVEN MongoDB vacío, WHEN se ejecuta seeder, THEN existen: Cliente A con 3 facturas (estados variados), Cliente B con 2 facturas, Cliente C con 3 facturas, Al menos 1 factura por estado en `primerrecordatorio` y `segundorecordatorio`."

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Poblar Datos Base de Desarrollo (Prioridad: P1)

Como desarrollador o tester del módulo de facturación, necesito ejecutar un proceso de carga inicial (seeder) que cree un conjunto mínimo, predecible y representativo de clientes y facturas, para poder probar listados, filtros y transiciones de estado sin tener que crear datos manualmente cada vez.

**Por qué esta prioridad**: Es el prerequisito habilitante para validar manualmente y en pruebas automatizadas el comportamiento del módulo de facturación (listar, filtrar por estado, transicionar). Sin datos representativos, las jornadas críticas del usuario no pueden demostrarse.

**Prueba Independiente**: Se puede probar partiendo de una base de datos vacía, ejecutando el seeder, y verificando mediante consulta el número de clientes, el número de facturas por cliente y la distribución de estados resultante. Entrega valor inmediato al habilitar un entorno de pruebas reproducible.

**Escenarios de Aceptación**:

1. **Dado** una base de datos vacía (sin clientes ni facturas), **Cuando** se ejecuta el seeder, **Entonces** existen exactamente 3 clientes identificables (Cliente A, Cliente B, Cliente C).
2. **Dado** una base de datos vacía, **Cuando** se ejecuta el seeder, **Entonces** el Cliente A tiene 3 facturas asociadas, el Cliente B tiene 2 facturas asociadas y el Cliente C tiene 3 facturas asociadas (8 facturas en total).
3. **Dado** una base de datos vacía, **Cuando** se ejecuta el seeder, **Entonces** las 3 facturas del Cliente A presentan estados variados (no todas en el mismo estado).
4. **Dado** una base de datos vacía, **Cuando** se ejecuta el seeder, **Entonces** existe al menos 1 factura en estado `primerrecordatorio` y al menos 1 factura en estado `segundorecordatorio` en el conjunto total.

---

### Historia de Usuario 2 - Ejecución Segura e Idempotente (Prioridad: P2)

Como desarrollador, quiero que el seeder sólo poble datos cuando la base está vacía y que una segunda ejecución no duplique registros, para mantener un estado de datos consistente y evitar inconsistencias durante el desarrollo.

**Por qué esta prioridad**: Evita la corrupción accidental del entorno de pruebas (datos duplicados o conteos inflados) cuando el seeder se ejecuta más de una vez, protegiendo la reproducibilidad de las pruebas.

**Prueba Independiente**: Se puede probar ejecutando el seeder dos veces consecutivas sobre una base de datos inicialmente vacía y verificando que los conteos de clientes y facturas permanecen idénticos tras la segunda ejecución.

**Escenarios de Aceptación**:

1. **Dado** una base de datos que ya contiene los datos sembrados, **Cuando** se ejecuta el seeder por segunda vez, **Entonces** los conteos de clientes (3) y facturas (8) no cambian y no se generan duplicados.
2. **Dado** una base de datos con datos preexistentes ajenos al seeder, **Cuando** se ejecuta el seeder, **Entonces** el seeder no inserta los datos base y reporta que la operación fue omitida.

---

### Casos Límite

- ¿Qué ocurre si el seeder se ejecuta sobre una base de datos parcialmente poblada (por ejemplo, clientes presentes pero sin facturas)? El seeder debe detectar el estado no-vacío y omitir la siembra para no mezclar datos.
- ¿Qué ocurre si la conexión a la base de datos no está disponible durante la ejecución? El seeder debe reportar el fallo de forma explícita sin dejar datos a medio insertar.
- ¿Qué ocurre con los campos de auditoría y recordatorios de cada factura sembrada (fechas de creación, cantidad de recordatorios) cuando su estado implica recordatorios ya enviados? Deben ser coherentes con el estado asignado.

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **RF-001**: El sistema DEBE proveer un proceso de siembra (seeder) ejecutable que cree datos iniciales de clientes y facturas.
- **RF-002**: El seeder DEBE crear exactamente 3 clientes distintos e identificables, referidos como Cliente A, Cliente B y Cliente C.
- **RF-003**: El seeder DEBE asociar 3 facturas al Cliente A, 2 facturas al Cliente B y 3 facturas al Cliente C, totalizando 8 facturas.
- **RF-004**: El seeder DEBE asignar estados variados a las facturas del Cliente A (no todas en el mismo estado).
- **RF-005**: El conjunto total de facturas sembradas DEBE incluir al menos una factura en estado `primerrecordatorio` y al menos una factura en estado `segundorecordatorio`.
- **RF-006**: Los estados asignados a las facturas DEBEN pertenecer al conjunto válido del ciclo de vida de factura (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`).
- **RF-007**: Cada factura sembrada DEBE tener valores coherentes en sus campos de auditoría y recordatorios respecto al estado que se le asigna (por ejemplo, una factura en `segundorecordatorio` refleja una cantidad de recordatorios y fechas consistentes con ese estado).
- **RF-008**: El seeder DEBE ejecutar la siembra únicamente cuando la base de datos esté vacía respecto a clientes y facturas; si ya existen datos, DEBE omitir la siembra sin modificar registros existentes.
- **RF-009**: El seeder DEBE ser idempotente: ejecuciones repetidas no deben duplicar registros ni alterar los conteos resultantes.
- **RF-010**: El seeder DEBE reportar el resultado de su ejecución (sembrado u omitido, y conteos resultantes) de forma observable.

### Entidades Clave

- **Cliente**: Parte a la que se le emiten facturas. Identificado de forma única; en este contexto se requieren tres instancias mínimas (A, B, C). Cada cliente se relaciona con una o más facturas.
- **Factura (Invoice)**: Registro de cobro asociado a un cliente. Posee un estado dentro del ciclo de vida, un monto, y campos de auditoría/recordatorios. Es la entidad cuya distribución de estados debe cumplir las condiciones de cobertura definidas. (Ver `005-invoice-entity` y `006-invoice-status-transitions`.)

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: Tras ejecutar el seeder sobre una base vacía, existen exactamente 3 clientes (100% del mínimo requerido).
- **CE-002**: Tras la siembra, la distribución de facturas por cliente es exactamente 3 / 2 / 3 (Cliente A / B / C), sumando 8 facturas.
- **CE-003**: Tras la siembra, el conjunto de facturas contiene al menos 1 factura en `primerrecordatorio` y al menos 1 en `segundorecordatorio` (cobertura mínima de estos dos estados = 100%).
- **CE-004**: Las 3 facturas del Cliente A presentan al menos 2 estados distintos entre sí (estados variados verificable).
- **CE-005**: Ejecutar el seeder dos veces consecutivas no incrementa los conteos de clientes ni de facturas (0% de duplicación).
- **CE-006**: El 100% de los estados de las facturas sembradas pertenecen al conjunto válido del ciclo de vida.

## Suposiciones

- Los estados válidos del ciclo de vida de la factura son los definidos en `006-invoice-status-transitions`: `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`.
- "Base de datos vacía" se interpreta como ausencia de clientes y facturas en las colecciones correspondientes; el seeder verifica esta condición antes de sembrar.
- El seeder está destinado a entornos de desarrollo y pruebas, no a producción; su propósito es habilitar pruebas reproducibles.
- "Estados variados" para el Cliente A se interpreta como al menos dos estados distintos entre sus 3 facturas; el conjunto total cubre, como mínimo, `primerrecordatorio` y `segundorecordatorio` según lo exigido.
- Los identificadores de los clientes (A, B, C) son etiquetas conceptuales; la generación de identificadores únicos reales se delega a la capa de persistencia.
- La entidad Invoice y sus reglas de estado ya existen (specs `005` y `006`); este seeder reutiliza ese modelo sin redefinirlo.
- Los montos asignados a las facturas son valores válidos (mayores o iguales a cero) coherentes con las reglas de la entidad Invoice.
