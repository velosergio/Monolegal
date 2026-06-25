# Investigación y Decisiones de Diseño: Entidad Invoice

**Relacionado con**: [plan.md](./plan.md)

## 1. Representación del Identificador (`Id`) en la Capa de Dominio

**Contexto**: El modelo de datos persistente utilizará MongoDB (según el contexto técnico del proyecto), el cual típicamente emplea `ObjectId` como tipo primario para las claves. Sin embargo, la constitución del proyecto dicta que el dominio no debe tener dependencias de infraestructura ni de frameworks de terceros (`MongoDB.Driver`).

**Decisión**: Utilizar `string` para la propiedad `Id` dentro de la entidad `Invoice` de la capa `Domain`.

**Razón**: 
1. Mantiene la capa de Dominio pura (Clean Architecture), libre de referencias a paquetes como `MongoDB.Bson`.
2. Facilita la portabilidad (si el sistema migrara a PostgreSQL en el futuro, un `string` puede mapear fácilmente a un UUID).
3. La capa de `Infrastructure`, a través de configuración y convenciones (ej. `BsonSerializer` o `BsonRepresentation(BsonType.ObjectId)` en un modelo de persistencia separado o mediante mapeo fluido), se encargará de traducir este `string` hacia y desde el `ObjectId` nativo de la base de datos sin contaminar el dominio.

**Alternativas Consideradas**:
- *Acoplamiento directo a `ObjectId`*: Rechazado categóricamente ya que viola el Principio I de la Constitución (Arquitectura Limpia).
- *Creación de un Struct fuertemente tipado `EntityId`*: Una abstracción útil, pero agrega complejidad adicional para el mapeo que podría no estar justificada en esta fase sin un análisis más amplio de las necesidades del sistema. Se opta por `string` por su simplicidad y bajo costo.

## 2. Gestión de Fechas de Auditoría (`CreatedAt` y `UpdatedAt`)

**Contexto**: La especificación exige que `CreatedAt` y `UpdatedAt` sean fechas gestionadas para auditoría.

**Decisión**: La entidad expondrá `CreatedAt` con un set privado y será inicializado en el constructor al momento de la instanciación. `UpdatedAt` podrá actualizarse mediante un método de mutación interno cada vez que se altere el estado significativo de la entidad. Utilizaremos `DateTime.UtcNow` para garantizar consistencia horaria global.

**Razón**:
Permite que el modelo de dominio controle su propio estado válido. Evita depender de mecanismos de persistencia (como triggers en bases de datos relacionales o hooks del driver de MongoDB) para asignar estas fechas esenciales, haciendo la lógica de negocio completamente testeable en memoria de forma aislada.
