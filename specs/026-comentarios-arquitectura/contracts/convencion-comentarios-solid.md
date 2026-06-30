# Contrato — Convención de comentarios SOLID en clase

Define el formato obligatorio del comentario a nivel de clase para las **clases clave** (ver `research.md` D2). Satisface FR-004 y FR-005.

## Formato

Comentario XML-doc de C#, en español, inmediatamente antes de la declaración de la clase:

```csharp
/// <summary>
/// <Responsabilidad de la clase en una sola frase>.
/// SOLID: <PRINCIPIO> — <justificación breve>[; <PRINCIPIO> — <justificación>].
/// </summary>
public sealed class NombreClase : IAbstraccion
{
    ...
}
```

## Reglas

1. El comentario DEBE nombrar al menos un principio: `SRP`, `OCP`, `LSP`, `ISP` o `DIP`.
2. Cada principio nombrado DEBE ir acompañado de una justificación concreta (no genérica).
3. Si la clase implementa una interfaz, el comentario DEBE evidenciar `DIP` u `OCP` referenciando la abstracción.
4. Redacción en **español**.
5. No duplicar la lógica en el comentario; describir la **razón de diseño**, no el algoritmo.

## Ejemplos (referencia)

```csharp
/// <summary>
/// Resuelve y aplica las transiciones de estado válidas de una factura.
/// SOLID: SRP — única razón de cambio: las reglas de transición de estado.
/// OCP — nuevas reglas se añaden sin modificar consumidores.
/// </summary>
public sealed class InvoiceTransitionService { ... }
```

```csharp
/// <summary>
/// Persistencia de facturas sobre MongoDB.
/// SOLID: DIP — implementa IInvoiceRepository; los casos de uso dependen de la abstracción, no de Mongo.
/// SRP — única responsabilidad: traducir entre el dominio y la colección de MongoDB.
/// </summary>
public sealed class MongoInvoiceRepository : IInvoiceRepository { ... }
```

## Verificación

- Code review: cada clase clave de la lista tiene el comentario con principio + justificación.
- Búsqueda: las clases clave aparecen al buscar `SOLID:` en `backend/` y `worker/`.
- `dotnet build` permanece verde (los comentarios no rompen compilación).
