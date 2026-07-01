# Roadmap Monolegal - Spec Driven Development

## Visión General

Desarrollar una plataforma de gestión de cobranza mediante iteraciones basadas en especificaciones técnicas claras, garantizando calidad, testabilidad y mantenibilidad en cada fase.

---

## 📋 Fase 0: Setup & Infrastructure [✅ - Hecho] `4/4 specs`

### Spec 0.1: Estructura de Proyectos

**GIVEN** el workspace vacío  
**WHEN** se ejecuta setup inicial  
**THEN** se deben crear:

- ✅ `backend/` con estructura clean architecture (Domain/Application/Infrastructure)
- ✅ `frontend/` con React + Vite + TypeScript
- ✅ `worker/` con host service
- ✅ `packages/shared/` con tipos compartidos
- ✅ `.dockerignore`, `docker-compose.yml`, `Dockerfile`

### Spec 0.2: Dependencias Backend

**GIVEN** proyecto ASP.NET Core creado  
**WHEN** se instalan dependencias  
**THEN** deben estar disponibles:

- ✅ ASP.NET Core 10
- ✅ MongoDB Driver
- ✅ FluentValidation
- ✅ Serilog
- ✅ Minimal APIs
- ✅ xUnit + Shouldly

### Spec 0.3: Dependencias Frontend

**GIVEN** proyecto React + Vite creado  
**WHEN** se instalan dependencias  
**THEN** deben estar disponibles:

- ✅ React 19+
- ✅ TypeScript
- ✅ Vite
- ✅ shadcn/ui
- ✅ TanStack Query
- ✅ Motion
- ✅ Vitest + Testing Library
- ✅ Biome (linting/formatting)

### Spec 0.4: MongoDB Connection

**GIVEN** Docker Compose configurado  
**WHEN** se ejecuta `docker-compose up`  
**THEN**:

- ✅ MongoDB está corriendo en puerto 27017
- ✅ Base de datos `monolegal_dev` existe
- ✅ Conexión verificada desde backend

---

## 🏗️ Fase 1: Domain & Data Layer [✅ - Hecho] `4/4 specs`

### Spec 1.1: Entidad Invoice (Dominio)

**GIVEN** necesidad de modelo de factura  
**WHEN** se define dominio  
**THEN** `Invoice` debe tener:

```csharp
Id: ObjectId
ClientId: string
Amount: decimal
Status: InvoiceStatus (enum)
CreatedAt: DateTime
UpdatedAt: DateTime
RemindersCount: int
LastReminderSentAt: DateTime?
```

### Spec 1.2: InvoiceStatus Transitions

**GIVEN** una factura creada  
**WHEN** se ejecutan transiciones  
**THEN** los estados permitidos son:

- ✅ `pending` → `primerrecordatorio` (tiempo configurable)
- ✅ `primerrecordatorio` → `segundorecordatorio` (tiempo configurable)
- ✅ `segundorecordatorio` → `desactivado` (tiempo configurable sin pago)
- ✅ Cualquier estado → `pagado` (manual o automático)
- ✅ Nueva pestaña en vista de configuración para definir los tiempos

### Spec 1.3: MongoDB Repository

**GIVEN** entidad Invoice en dominio  
**WHEN** se implementa repositorio  
**THEN** debe soportar:

- ✅ `GetByStatusAsync(InvoiceStatus)` - obtener facturas por estado
- ✅ `GetByClientIdAsync(clientId)` - obtener facturas por cliente
- ✅ `UpdateStatusAsync(id, newStatus)` - cambiar estado
- ✅ `InsertAsync(invoice)` - crear factura
- ✅ Índices en `Status` y `ClientId`

### Spec 1.4: Seed Data - 3 Clientes Mínimo

**GIVEN** MongoDB vacío  
**WHEN** se ejecuta seeder  
**THEN** existen:

- ✅ Cliente A con 3 facturas (estados variados)
- ✅ Cliente B con 2 facturas
- ✅ Cliente C con 3 facturas
- ✅ Al menos 1 factura por estado en `primerrecordatorio` y `segundorecordatorio`

---

## 📡 Fase 2: Backend API (Minimal APIs) [✅ - Hecho] `2/2 specs`

### Spec 2.1: inovice api endpoints

#### GET /api/invoices (Lista)

**GIVEN** facturas en MongoDB  
**WHEN** se llama `GET /api/invoices`  
**THEN** respuesta:

```json
{
  "data": [
    {
      "id": "...",
      "clientId": "...",
      "amount": 100.50,
      "status": "primerrecordatorio",
      "createdAt": "2026-01-01T..."
    }
  ],
  "total": 8,
  "pageSize": 10
}
```

- ✅ Query params: `?status=primerrecordatorio&page=1&pageSize=10`
- ✅ Status 200 OK

####  GET /api/invoices/{id} (Detalle)

**GIVEN** ID de factura válido  
**WHEN** se llama `GET /api/invoices/{id}`  
**THEN**:

- ✅ Status 200 con objeto completo
- ✅ Status 404 si no existe

#### POST /api/invoices/transition/{id}

**GIVEN** factura en estado válido  
**WHEN** se llama `POST /api/invoices/transition/{id}` con `{ "newStatus": "segundorecordatorio" }`  
**THEN**:

- ✅ Estado se actualiza en BD
- ✅ Status 200 con factura actualizada
- ✅ Status 400 si transición no permitida

#### GET /api/invoices/stats (Dashboard)

**GIVEN** facturas en BD  
**WHEN** se llama `GET /api/invoices/stats`  
**THEN** respuesta:

```json
{
  "totalInvoices": 8,
  "byStatus": {
    "pending": 1,
    "primerrecordatorio": 2,
    "segundorecordatorio": 3,
    "pagado": 2
  },
  "byClient": {
    "clientA": 3,
    "clientB": 2,
    "clientC": 3
  }
}
```

### Spec 2.2: Swagger/OpenAPI

**GIVEN** endpoints implementados  
**WHEN** se accede a `/swagger`  
**THEN**:

- ✅ Todos los endpoints documentados
- ✅ Modelos y DTO visibles
- ✅ Try-it-out funcional

---

## ⚙️ Fase 3: Worker & Email Service `4/4 specs`

### Spec 3.1: Email Service Interface

**GIVEN** necesidad de enviar correos
**WHEN** se define contrato
**THEN** interfaz `IEmailService`:

```csharp
Task SendReminderAsync(string clientEmail, Invoice invoice)
Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice)
```

### Spec 3.2: Hosted Service - State Transitions ✅ (implementada — `specs/012-worker-state-transitions`)

**GIVEN** worker corriendo  
**WHEN** se ejecuta cada X minutos (configurable vía `InvoiceTransitionsWorker:IntervalMinutes`)  
**THEN**:

- ✅ Busca facturas en `primerrecordatorio`/`segundorecordatorio` según umbrales de días configurables (admin)
- ✅ Ejecuta transiciones automáticas (delegando en `InvoiceTransitionService`)
- ✅ Aísla errores por factura (un fallo no aborta el lote)
- ✅ Registra cada ciclo en logs Serilog (timestamp, evaluadas, transicionadas, errores, duración)

### Spec 3.3: Email Sending on Transition

**GIVEN** transición de estado  
**WHEN** worker procesa factura  
**THEN**:

- ✅ Envía correo con template según nuevo estado
- ✅ Actualiza `LastReminderSentAt`
- ✅ Incrementa `RemindersCount`
- ✅ Registra éxito/error en BD

### Spec 3.4: Logging & Monitoring

**GIVEN** worker ejecutándose  
**WHEN** procesa facturas  
**THEN**:

- ✅ Serilog registra: timestamp, factura, estado anterior/nuevo, resultado email
- ✅ Logs persistidos (file o cloud)
- ✅ Estructurado en formato JSON

---

## 🎨 Fase 4: Frontend (React UI) `9/9 specs`

### Spec 4.1: Layout Base ✅ Implementada (feature 014-admin-panel-invoices)

**GIVEN** aplicación React iniciada  
**WHEN** se carga `/`  
**THEN**:

- ✅ Navbar con logo Monolegal
- ✅ Sidebar con navegación
- ✅ Footer con info
- ✅ Responsive (mobile/desktop)

### Spec 4.2: Invoices Page - Listado ✅ Implementada (feature 014-admin-panel-invoices)

**GIVEN** usuario en `/invoices`  
**WHEN** página carga  
**THEN**:

- ✅ Tabla con columnas: ID, Cliente, Monto, Estado, Última Acción
- ✅ Filtro por estado (dropdown)
- ✅ Búsqueda por cliente
- ✅ Paginación (10 items/página)
- ✅ Skeleton loaders mientras carga

### Spec 4.3: Invoice Detail Modal ✅ Implementada (feature 015-admin-panel-detail-dashboard)

**GIVEN** usuario hace click en fila de tabla  
**WHEN** modal se abre  
**THEN**:

- ✅ Muestra todos los campos de factura
- ✅ Historial de cambios de estado
- ✅ Botón "Cambiar Estado" (solo si es transición válida)
- ✅ Datos actualizados via TanStack Query

### Spec 4.4: Dashboard / Stats ✅ Implementada (feature 015-admin-panel-detail-dashboard)

**GIVEN** usuario en `/dashboard`  
**WHEN** página carga  
**THEN**:

- ✅ Cards con stats: total, por estado, por cliente
- ✅ Gráficos (motion animados)
- ✅ Último refresh mostrado

### Spec 4.5: Form - Manual State Transition ✅ Implementada (feature 016-transition-form-donut-dashboard)

**GIVEN** modal de detalle abierto  
**WHEN** usuario selecciona nuevo estado y confirma  
**THEN**:

- ✅ Form validación frontend
- ✅ POST a `/api/invoices/transition/{id}`
- ✅ Toast de éxito/error
- ✅ Tabla y modal se actualizan

### Spec 4.6: Vista Configuración + Resend API + Tools ✅ Implementada (feature 017-configuracion-resend-tools)

**GIVEN** usuario en `/configuracion`  
**WHEN** página carga  
**THEN**:

- ✅ Sección de configuración de Resend API (API key, dominio remitente)
- ✅ Gestión de plantillas de email (asunto, cuerpo, variables)
- ✅ Botón de prueba para enviar email de test
- ✅ Tools/utilidades de administración (reenvío manual, limpieza de cola)
- ✅ Validación de credenciales con feedback (toast éxito/error)
- ✅ Persistencia de ajustes vía API

### Spec 4.7: CRUD Facturas ✅ Implementada (feature 018-crud-facturas-clientes)

**GIVEN** usuario en `/invoices`  
**WHEN** gestiona facturas  
**THEN**:

- ✅ Crear factura (form con validación: cliente, monto, items, vencimiento)
- ✅ Editar factura existente (datos no bloqueados por estado)
- ✅ Eliminar factura (confirmación modal)
- ✅ POST/PUT/DELETE a `/api/invoices`
- ✅ Toast de éxito/error en cada operación
- ✅ Tabla y dashboard se actualizan vía TanStack Query

### Spec 4.8: CRUD Clientes ✅ Implementada (feature 018-crud-facturas-clientes)

**GIVEN** usuario en `/clientes`  
**WHEN** gestiona clientes  
**THEN**:

- ✅ Listado de clientes con búsqueda y paginación
- ✅ Crear cliente (form con validación: nombre, email, datos de contacto)
- ✅ Editar cliente existente
- ✅ Eliminar cliente (confirmación modal, valida facturas asociadas)
- ✅ POST/PUT/DELETE a `/api/clients`
- ✅ Toast de éxito/error y refresco automático del listado

### Spec 4.9: Vista Envíos ✅ Implementada (feature 019-vista-envios)

**GIVEN** usuario en `/envios`  
**WHEN** página carga  
**THEN**:

- ✅ Listado de facturas con su estado de envío (pendiente, enviado, fallido, reintentando)
- ✅ Columnas: ID, Cliente, Email, Estado de envío, Último intento, Reintentos
- ✅ Filtro por estado de envío y búsqueda por cliente/email
- ✅ Acciones manuales: reenviar email, reintentar fallidos, cancelar envío
- ✅ POST a endpoints de reenvío/reintento (`/api/invoices/{id}/resend`)
- ✅ Indicadores visuales (badges de color) por estado de envío
- ✅ Toast de éxito/error y refresco automático vía TanStack Query
- ✅ Estados de carga (skeletons) y vacíos (empty states)

---

## 🧪 Fase 5: Testing & Quality `5/5 specs`

### Spec 5.1: Unit Tests - Domain ✅ Implementada (feature 020-test-unitarios-dominio)

**GIVEN** entidades de dominio  
**WHEN** se ejecutan tests  
**THEN** cobertura:

- ✅ `InvoiceStatus` transitions valid/invalid (xUnit)
- ✅ Invoice creation con validaciones
- ✅ Shouldly para lectibilidad
- ✅ Mínimo 85% cobertura

### Spec 5.2: Integration Tests - API ✅ Implementada (feature 021-integration-tests-api) ✅ Implementada (feature 021-integration-test-api)

**GIVEN** endpoints implementados  
**WHEN** se ejecutan tests  
**THEN**:

- ✅ GET /api/invoices retorna 200
- ✅ Filtro por status funciona
- ✅ POST transition valida estado permitido
- ✅ 404 en ID no existente
- ✅ WebApplicationFactory para setup

### Spec 5.3: Frontend Component Tests ✅ Implementada (feature 022-tests-componentes-frontend)

**GIVEN** componentes React  
**WHEN** se corren tests con Vitest  
**THEN**:

- ✅ Renderiza sin errores
- ✅ Interacciones simuladas (click, select)
- ✅ Async handlers (TanStack Query mocked)
- ✅ Snapshot tests para UI crítica

### Spec 5.4: E2E Tests - Playwright ✅ Implementada (feature 023-tests-e2e-playwright)

**GIVEN** aplicación fullstack  
**WHEN** se corren E2E tests  
**THEN** flujos críticos:

- ✅ Abrir lista de facturas
- ✅ Filtrar por estado
- ✅ Hacer transición manual
- ✅ Ver dashboard actualizado

### Spec 5.5: Test Runner Unificado ✅ Implementada (feature 024-test-runner-unification)

**GIVEN** los distintos sistemas de test del proyecto (backend xUnit, frontend Vitest, E2E Playwright)  
**WHEN** se ejecuta un único comando (ej. `npm run test:all` o script `test-all`)  
**THEN**:

- ✅ Corre los tests del backend (`dotnet test`)
- ✅ Corre los tests del frontend (`vitest run`)
- ✅ Corre los tests E2E (`playwright test`)
- ✅ Ejecuta todas las suites con un solo comando multiplataforma (Windows/Linux)
- ✅ Reporta resultado consolidado (PASS/FAIL por suite) y código de salida agregado
- ✅ Falla el comando completo si cualquier suite falla (apto para CI)


---

## 🚀 Fase 6: Documentation `2/2 specs`

### Spec 6.1: API Documentation ✅ Implementada (feature 025-documentacion-api)

**GIVEN** proyecto finalizado  
**WHEN** se lee documentación  
**THEN** incluye:

- ✅ Architecture overview
- ✅ Entity relationship diagrams
- ✅ API endpoint reference
- ✅ Setup instructions
- ✅ Deployment guide
- ✅ Coleccion Postman
- ✅ Swagger UI con boton en el sidebar

### Spec 6.2: Code Comments & Architecture Doc ✅ Implementada (feature 026-comentarios-arquitectura)

**GIVEN** código implementado  
**WHEN** se revisa  
**THEN**:

- ✅ Clean Architecture explicada en README
- ✅ SOLID principles aplicados (comentarios en clase)
- ✅ Dependency Injection claramente mapeado
- ✅ Decision records (ADR) para cambios importantes

---

## 📊 Matriz de Aceptación

> Leyenda: ✅ Hecho · 🟡 En progreso · ⬜ Pendiente

| Fase | Spec | Progreso | Status |
|------|------|----------|--------|
| 0    | 0.1-0.4 | 4/4 | ✅ |
| 1    | 1.1-1.4 | 4/4 | ✅ |
| 2    | 2.1-2.2 | 2/2 | ✅ |
| 3    | 3.1-3.4 | 4/4 | ✅ |
| 4    | 4.1-4.9 | 9/9 | ✅ |
| 5    | 5.1-5.5 | 5/5 | ✅ |
| 6    | 6.1-6.2 | 2/2 | ✅ |

---

## 🎯 Criterios de Éxito Globales

✅ **Code Quality**: 85%+ cobertura tests, 0 critical Sonar issues  
✅ **Performance**: Queries < 200ms, Frontend TTI < 2s  
✅ **Security**: Validación FluentValidation en todos endpoints, CORS configurado  
✅ **Scalability**: Docker ready, stateless APIs, MongoDB connection pooling  
✅ **Maintainability**: Clean Architecture, SOLID, DI container, comentarios estratégicos  
✅ **UX**: Responsive, dark mode, loading states, error handling elegante


## Anotaciones:
* Fueron 846 tareas ejecutadas en 1 semana de desarrollo
* Se uso Cursor, Kiro y Claude Code

* vista del modal /facturas:
  - mejoras en el UX, implementar busqueda en tiempo real en vez del droplist en clientes
  - mejora en el ux, implementar selector de fecha / calendario "nativo del navegador/sistema" al precionar en la fecha
* vista del modal /clientes:
  - habilitar editar al dar click en el nombre