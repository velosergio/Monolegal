# ADR 0003 — Repositorios y cliente de MongoDB con ciclo de vida Singleton

**Estado**: Aceptada · **Fecha**: 2026-06-30 · **Feature**: 026 — Comentarios de Código y Documentación de Arquitectura

> ADR retroactivo: documenta una decisión estructural ya vigente en el código.

## Contexto

El acceso a datos se hace con el driver oficial de MongoDB. Una pregunta de diseño no obvia es el
**ciclo de vida** de los repositorios (`MongoInvoiceRepository`, `MongoClientRepository`,
`MongoSystemSettingsRepository`) y del `IMongoClient`/`IMongoDatabase` en el contenedor de DI.

El patrón por defecto en muchas aplicaciones web (especialmente con Entity Framework) es `Scoped`
(una instancia por request). Sin embargo, el driver de MongoDB no es EF: el `MongoClient` es
*thread-safe* y mantiene internamente su propio *connection pool*; crear uno por request es un
antipatrón que agota conexiones.

## Decisión

Registrar el `IMongoClient`, el `IMongoDatabase` y los repositorios como **Singleton** en
`backend/Infrastructure/Configuration/DependencyInjection.cs`. El `MongoClient` se construye con
`MaxConnectionPoolSize` y `ServerSelectionTimeout` explícitos. Los repositorios son *stateless*
(solo envuelven colecciones), por lo que compartir una instancia es seguro.

## Alternativas consideradas

- **Repositorios `Scoped`**: alinearía con el hábito EF, pero no aporta aislamiento real (los
  repositorios no tienen estado por request) y arriesga crear clientes/pools redundantes. Descartada.
- **`Transient`**: instanciación por resolución, sin beneficio y con sobrecoste. Descartada.

## Consecuencias

- **Positivas**: un único *connection pool* reutilizado; menor presión de GC; coherente con las guías
  del driver de MongoDB; el worker aplica el mismo criterio.
- **Negativas / costes**: cualquier estado mutable que se añadiera a un repositorio debería ser
  *thread-safe*; los repositorios deben permanecer *stateless* por contrato.
