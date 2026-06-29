# Quickstart: Tests de Integración de la API

**Feature**: 021-integration-tests-api | **Fecha**: 2026-06-29

Guía para ejecutar y validar la suite de integración HTTP de los endpoints de facturas. Los detalles de casos están en [contracts/api-test-matrix.md](./contracts/api-test-matrix.md) y las formas de respuesta en [data-model.md](./data-model.md).

## Prerrequisitos

- SDK de .NET 10 instalado (`dotnet --version` ⇒ 10.x).
- **No** se requiere MongoDB ni docker para esta suite: las pruebas sustituyen los repositorios por dobles en memoria (ver research.md D2).
- Restaurar dependencias del backend (la primera vez): `dotnet restore backend/Tests/Tests.csproj`.

## Ejecutar la suite

Toda la suite de tests del backend (incluye los nuevos tests de integración del API):

```bash
dotnet test backend/Tests/Tests.csproj
```

Sólo los tests de integración de los endpoints de facturas (por clase):

```bash
dotnet test backend/Tests/Tests.csproj --filter "FullyQualifiedName~InvoiceApiEndpointsTests"
```

Sólo la categoría sin dependencias externas (rápida, apta para CI sin Mongo):

```bash
dotnet test backend/Tests/Tests.csproj --filter "Category=Application"
```

## Resultado esperado

- Todos los casos de la matriz (US1–US4) en verde.
- Cero pruebas omitidas (sin `Skip`/`[Ignore]`), conforme al Principio IV.
- Corrida de la clase `InvoiceApiEndpointsTests` en pocos segundos (sin E/S de red ni base de datos).

## Validar los criterios de la spec

| Criterio (spec.md) | Cómo se valida |
|--------------------|----------------|
| SC-001 (éxito de cada endpoint) | Casos #1, #7, #10, #15/#16 verdes |
| SC-002 (rutas de error) | Casos #4, #5, #6, #8, #9, #11, #12, #13, #14 verdes |
| SC-003 (transición persiste/no cambia) | Casos #10 (persiste) y #11 (sin cambios) verdes |
| SC-004 (repetible/aislado) | Ejecutar la suite 2 veces seguidas ⇒ resultado idéntico |
| SC-005 (cero fallos/omitidos en CI) | Salida de `dotnet test` con 0 failed / 0 skipped |

## Verificar determinismo (SC-004)

Ejecutar dos veces y comparar el conteo de passed/failed:

```bash
dotnet test backend/Tests/Tests.csproj --filter "FullyQualifiedName~InvoiceApiEndpointsTests"
dotnet test backend/Tests/Tests.csproj --filter "FullyQualifiedName~InvoiceApiEndpointsTests"
```

Ambas corridas deben reportar el mismo número de pruebas pasadas, sin fallos intermitentes (sin dependencia del orden de ejecución).

## Notas de integración con CI

- La clase usa `[Trait("Category", "Application")]`: el pipeline puede ejecutarla sin provisionar MongoDB.
- Un fallo de la suite hace fallar el `dotnet test` (código de salida ≠ 0), bloqueando el merge conforme al CI Gate del Principio IV.
