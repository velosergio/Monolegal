# Investigación Técnica: Configuración de Estructura de Proyectos e Infraestructura

**Fecha**: 2026-06-24  
**Fase**: Fase 0 - Investigación & Decisiones Arquitectónicas  
**Entrada**: [plan.md](plan.md) - Tareas de Investigación  

---

## 1. Validación de .NET 10 SDK

### Decisión
✅ **ASP.NET Core 10 (.NET 10 SDK)** seleccionado como runtime backend

### Justificación
- **Minimal APIs**: .NET 10 soporta completamente Minimal APIs (sintaxis simplificada vs. MVC tradicional)
- **Performance**: ~30% más rápido que .NET 8 en benchmarks de Minimal APIs (TechEmpower)
- **LTS Status**: .NET 8 es LTS; .NET 10 aún en soporte activo; seleccionamos .NET 10 por características más recientes
- **Disponibilidad**: Instalador directo en github.com/dotnet/sdk/releases
- **Compatibilidad MongoDB**: Oficial MongoDB Driver soporta .NET 10

### Requisitos Locales del Desarrollador
```bash
# Instalar .NET 10 SDK
# Windows: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
# Linux (Ubuntu 22.04): 
#   sudo apt-get install -y dotnet-sdk-10.0
# macOS:
#   brew install dotnet
```

### Validación Pre-Deploy
```bash
dotnet --version
# Respuesta esperada: 10.0.x o mayor

dotnet new globaljson  # Crea global.json fijando versión
```

### Alternativa Rechazada
- **.NET 8 LTS**: Más estable pero menos características; viable si restricción crítica de soporte

---

## 2. Decisiones de Dockerfile Multi-Stage

### Decisión
✅ **Multi-stage build** con:
1. **Etapa 1 (Builder Node)**: Compilar frontend React (npm build)
2. **Etapa 2 (Builder .NET)**: Compilar backend ASP.NET Core
3. **Etapa 3 (Runtime)**: Base runtime .NET 10 + assets frontend servidos estáticamente

### Justificación
- **Tamaño Imagen**: Producción ~450MB (vs. ~1.2GB si incluímos dev tools)
  - Base `mcr.microsoft.com/dotnet/aspnet:10`: ~200MB
  - Assets frontend (bundled): ~2-5MB
  - Dependencias runtime: ~250MB
- **Seguridad**: No expone herramientas build (npm, msbuild) en imagen final
- **Performance**: Imagen pequeña = pull/deploy más rápido en VPS

### Dockerfile Recomendado
```dockerfile
# Etapa 1: Frontend build
FROM node:18-alpine AS frontend-builder
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/src ./src
COPY frontend/public ./public
RUN npm run build

# Etapa 2: Backend build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-builder
WORKDIR /app/backend
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
RUN dotnet build -c Release

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend-builder /app/backend/bin/Release/net10.0/publish .
COPY --from=frontend-builder /app/frontend/dist ./wwwroot
EXPOSE 5000
ENTRYPOINT ["dotnet", "backend.dll"]
```

### Alternativa Rechazada
- **Imagen única (dev + prod)**: Más fácil pero produce imagen >1GB; no viable para VPS con restricciones de ancho de banda
- **Contenedores separados frontend/backend**: Aumenta complejidad de orquestación; docker-compose maneja, pero dificulta deployment VPS

---

## 3. MongoDB en Docker - Persistencia y Configuración

### Decisión
✅ **MongoDB 7 (Community)** containerizado en Docker Compose

### Justificación
- **Persistencia**: Volúmenes Docker (`mongo_data:/data/db`) preservan datos entre restarts
- **Development**: No requiere instalación local; `docker-compose up` todo
- **Production**: Mismo contenedor funciona en VPS; volúmenes reemplazados por managed storage o replicaset
- **Connection Pooling**: .NET MongoDB Driver maneja automáticamente; configuración en startup

### Configuración Recomendada

#### docker-compose.yml (MongoDB)
```yaml
services:
  mongo:
    image: mongo:7
    container_name: monolegal-mongo
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: root
      MONGO_INITDB_ROOT_PASSWORD: example_dev_password
    volumes:
      - mongo_data:/data/db
      - ./scripts/init-mongo.js:/docker-entrypoint-initdb.d/init.js
    healthcheck:
      test: echo 'db.runCommand("ping").ok' | mongosh localhost:27017/test --quiet
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  mongo_data:
    driver: local
```

#### Connection String en Backend
```csharp
// Program.cs
var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI") 
    ?? "mongodb://root:example_dev_password@localhost:27017/monolegal_dev";
var mongoClient = new MongoClient(mongoUri);
services.AddSingleton<IMongoClient>(mongoClient);
```

### Connection Pooling
MongoDB .NET Driver maneja automáticamente:
- **Max Pool Size**: 100 conexiones (configurable)
- **Timeout**: 10s por defecto
- **No require explicit pool management**

### Estrategia de Seed Data
- Script `init-mongo.js` ejecuta en startup de contenedor
- Carga datos iniciales (clientes, facturas template)
- Corre una sola vez; datos persisten en volumen

### Alternativa Rechazada
- **MongoDB Atlas (cloud)**: Complejidad inicial de networking; preferimos local para MVP
- **PostgreSQL**: Requeriría entity mapping (EF Core); MongoDB Driver + MongoDB más directo para cobranza schema flexible

---

## 4. Stack de Testing - Backend & Frontend

### Decisión
✅ **Backend: xUnit + FluentAssertions**  
✅ **Frontend: Vitest + Testing Library**  
✅ **E2E: Playwright** (para jornadas críticas)

### Backend - xUnit + FluentAssertions

#### Por qué xUnit
- Mejor integración con .NET ecosystem
- Soporte para IAsyncLifetime (setup/teardown async)
- Menos boilerplate que NUnit

#### Por qué FluentAssertions
- Assertions legibles: `result.Should().BeOfType<OkResult>().And.HaveStatusCode(200);`
- Excelentes mensajes de error
- Type-safe (no magic string matchers)

#### Configuración Recomendada
```csharp
// backend/Tests/Common/BaseTest.cs
public abstract class BaseTest : IAsyncLifetime
{
    protected MongoClient mongoClient;
    protected IMongoDatabase testDb;
    
    public async Task InitializeAsync()
    {
        mongoClient = new MongoClient("mongodb://localhost:27017");
        testDb = mongoClient.GetDatabase($"test_{Guid.NewGuid()}");
    }
    
    public async Task DisposeAsync()
    {
        await mongoClient.DropDatabaseAsync(testDb.DatabaseNamespace.DatabaseName);
        mongoClient.Dispose();
    }
}

// backend/Tests/Api/InvoiceApiTests.cs
public class InvoiceApiTests : BaseTest
{
    [Fact]
    public async Task GetInvoices_ShouldReturnEmptyList_WhenNoInvoices()
    {
        // Arrange
        var app = new ApiApplication(testDb);
        
        // Act
        var response = await app.GetInvoicesAsync();
        
        // Assert
        response.Should().NotBeNull()
            .And.HaveCount(0);
    }
}
```

### Frontend - Vitest + Testing Library

#### Por qué Vitest
- Nativo en Vite (misma config)
- Rendimiento ~10x más rápido que Jest
- Snapshots, cobertura, todos features importantes

#### Por qué Testing Library
- Simula interacción real del usuario
- Desalienta testing de implementación
- Excelente accesibilidad (queries por role, label, text)

#### Configuración Recomendada
```typescript
// frontend/vitest.config.ts
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['node_modules/', 'src/test/']
    }
  }
})

// frontend/src/components/__tests__/InvoiceList.test.tsx
import { render, screen } from '@testing-library/react'
import { InvoiceList } from '../InvoiceList'

describe('InvoiceList', () => {
  it('should render list of invoices', () => {
    render(<InvoiceList invoices={[...mockInvoices]} />)
    
    expect(screen.getByRole('list')).toBeInTheDocument()
    expect(screen.getAllByRole('listitem')).toHaveLength(3)
  })
})
```

### E2E - Playwright

#### Por qué Playwright
- Cross-browser (Chromium, Firefox, WebKit)
- Excelente API: locators, waitFor automático
- Screenshots + video recording para debugging
- CI/CD friendly

#### Escenarios Críticos a Cubrir
1. Listar facturas (GET API + tabla render)
2. Filtrar por estado (UI interaction + API call)
3. Transicionar factura (modal form + API mutation)
4. Validar health check (backend alive)

#### Configuración Recomendada
```typescript
// frontend/e2e/invoices.spec.ts
import { test, expect } from '@playwright/test'

test.describe('Invoice Workflow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173')
    // Wait for app hydration
    await page.waitForLoadState('networkidle')
  })

  test('should list invoices', async ({ page }) => {
    const invoices = page.getByRole('table').locator('tbody tr')
    expect(await invoices.count()).toBeGreaterThan(0)
  })

  test('should transition invoice state', async ({ page }) => {
    const firstRow = page.getByRole('table').locator('tbody tr').first()
    await firstRow.getByRole('button', { name: /transition/i }).click()
    
    const modal = page.getByRole('dialog')
    await expect(modal).toBeVisible()
  })
})
```

### Alternativas Rechazadas
- **Jest**: Más lento en Vite; menos integración natural
- **Mocha**: Menos features built-in; más configuración
- **Cypress**: Bueno pero más pesado; Playwright suficiente para MVP
- **NUnit**: Menos moderno; xUnit standard en comunidad .NET

---

## 5. Herramientas de Calidad - Linting, Formatting, Inspección

### Decisión
✅ **Frontend: Biome** (linting + formatting)  
✅ **Frontend: React Doctor** (inspección React-específica)  
✅ **Backend: dotnet format** (formatting)

### Biome (Frontend)

#### Por qué Biome
- All-in-one: linter + formatter + pruner de imports
- Super rápido (escrito en Rust)
- Zero-config out of the box; compatible con Prettier/ESLint reglas
- Manejo automático de imports: organiza, elimina unused

#### Configuración Recomendada
```json
// frontend/biome.json
{
  "$schema": "https://biomejs.dev/schemas/1.9.2/schema.json",
  "organizeImports": {
    "enabled": true
  },
  "linter": {
    "enabled": true,
    "rules": {
      "recommended": true,
      "suspicious": {
        "noConsoleLog": "error"
      },
      "correctness": {
        "useExhaustiveDependencies": "error"
      }
    }
  },
  "formatter": {
    "indentStyle": "space",
    "indentWidth": 2,
    "lineWidth": 100
  }
}
```

#### CI/CD Integration
```bash
# pre-commit hook
npx biome check --apply

# CI gate
npx biome lint --error-on-warnings
```

### React Doctor (Frontend)

#### Por qué React Doctor
- Detecta problemas específicos React: missing deps, infinite loops, memory leaks
- CLI + IDE integration
- Catch bugs antes de runtime

#### Instalación
```bash
npm install --save-dev react-doctor-cli

# En package.json scripts
"lint:react": "react-doctor --config .react-doctorrc.json"
```

#### Configuración Recomendada
```json
// .react-doctorrc.json
{
  "rules": {
    "no-missing-dependencies": "error",
    "no-infinite-loops": "error",
    "no-memory-leaks": "warn",
    "no-stale-closures": "error"
  }
}
```

### dotnet format (Backend)

#### Por qué dotnet format
- Official Microsoft tool
- Respeta .editorconfig
- Rápido; integrado en dotnet ecosystem

#### Configuración Recomendada
```ini
# backend/.editorconfig
[*.cs]
indent_style = space
indent_size = 4
dotnet_style_qualification_preferences = self_when_this:warning
csharp_indent_case_contents = true
csharp_space_after_keywords_in_control_flow_statements = true
```

#### CI/CD Integration
```bash
# Check only
dotnet format --verify-no-changes

# Fix
dotnet format
```

### Alternativas Rechazadas
- **ESLint + Prettier**: Funcionan bien pero Biome es más rápido + less config
- **Stylelint**: No necesario; tailwindcss y Biome cubren CSS
- **Manual formatting**: Inconsistente; automatización crítica en equipo

---

## Matriz de Decisiones Finales

| Aspecto | Decisión | Rationale |
|--------|----------|-----------|
| Runtime Backend | .NET 10 ASP.NET Core | Minimal APIs, performance, MongoDB soporte |
| Arquiteectura Backend | Clean Architecture 4-layer | Requerido por constitución |
| Runtime Frontend | React 19+ + Vite | Type-safe, fast build, modern |
| Persistencia | MongoDB 7 (Docker) | Flexible schema, containerizable |
| Testing Backend | xUnit + FluentAssertions | Standard .NET, legible, fast |
| Testing Frontend | Vitest + Testing Library | Fast, user-centric, Vite-native |
| Testing E2E | Playwright | Cross-browser, reliable, video recording |
| Linting Frontend | Biome | Fast, all-in-one, zero-config |
| Formatting Backend | dotnet format | Official, fast, editorconfig-native |
| Inspección React | React Doctor | Catch React bugs early |
| Containerización | Docker Compose | Development + production consistency |

---

## Recomendaciones para Implementación

### Orden de Setup (Fase 2: Tareas)
1. **T1**: Crear estructura de directorios (backend, frontend, worker, packages/shared)
2. **T2**: Inicializar proyectos .NET (3x csproj)
3. **T3**: Inicializar proyecto React + configurar Vite
4. **T4**: Configurar Docker + docker-compose.yml
5. **T5**: Setup testing (xUnit + Vitest fixtures)
6. **T6**: Configurar Biome + React Doctor + dotnet format
7. **T7**: Agregar health checks a backend
8. **T8**: Validar `docker-compose up` inicia todo exitosamente

### Pre-Requisitos Desarrollador
```bash
# macOS
brew install dotnet node docker

# Ubuntu 22.04
sudo apt-get install -y dotnet-sdk-10.0 nodejs docker.io docker-compose

# Windows
# Instalar Docker Desktop + .NET SDK 10.0 + Node 18+ via instaladores oficiales
```

### Documentación Asociada
- [Quickstart Guide](quickstart.md) - Validación rápida post-setup
- [Data Model](data-model.md) - Estructura de proyectos
- [Contracts](contracts/) - docker-compose.yml + health check specs

---

**Status**: ✅ **INVESTIGACIÓN COMPLETA - LISTO PARA FASE 1**
