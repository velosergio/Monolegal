# Validación Rápida: Estructura de Proyectos e Infraestructura

**Fase**: Fase 1 - Validación  
**Entrada**: [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/docker-compose.yml](contracts/docker-compose.yml)  
**Objetivo**: Verificar que toda la estructura de infraestructura está correctamente configurada y funcional

---

## Prerequisitos

Antes de comenzar, verificar que el ambiente local tiene:

```bash
# Verificar Docker
docker --version
# Esperado: Docker version 20.10+ o Docker Desktop 4.0+

# Verificar Docker Compose
docker-compose --version
# Esperado: Docker Compose version 2.0+

# Verificar .NET 10 SDK
dotnet --version
# Esperado: 10.0.x o superior

# Verificar Node.js
node --version
# Esperado: v18.0.0 o superior

npm --version
# Esperado: 9.0.0 o superior
```

### Sistema Operativo Compatible
- ✅ **macOS**: 12+ con Docker Desktop
- ✅ **Ubuntu/Debian**: 22.04 LTS con Docker Engine + Docker Compose
- ✅ **Windows**: 10/11 con Docker Desktop (WSL 2 mode)

---

## Setup Inicial (< 2 minutos)

### Paso 1: Clonar Repositorio
```bash
git clone https://github.com/tu-org/monolegal.git
cd monolegal
```

### Paso 2: Iniciar Servicios Docker
```bash
# Build e iniciar todos los contenedores
docker-compose up -d --build

# Esperar ~30 segundos para que servicios inicien completamente
# (especialmente MongoDB con init script)

# Verificar estado de contenedores
docker-compose ps
```

**Salida esperada**:
```
NAME                COMMAND             SERVICE    STATUS        PORTS
monolegal-frontend  npm run dev        frontend   Up (healthy)   0.0.0.0:5173->5173/tcp
monolegal-backend   dotnet backend.dll backend    Up (healthy)   0.0.0.0:5000->5000/tcp
monolegal-worker    dotnet worker.dll  worker     Up             
monolegal-mongo     mongosh mongodb... mongo      Up (healthy)   0.0.0.0:27017->27017/tcp
```

Si algún contenedor está **Exited** o **Unhealthy**:
```bash
# Ver logs del servicio problemático
docker-compose logs backend  # (o frontend, worker, mongo)

# Restart específico
docker-compose restart backend
```

### Paso 3: Verificar Estructura Local

```bash
# Verificar directorios creados
ls -la

# Esperado:
# backend/              (ASP.NET Core project)
# frontend/             (React + Vite project)
# worker/               (Background service)
# packages/shared/      (Shared DTOs/types)
# docker-compose.yml    (Orquestación)
# Dockerfile            (Multi-stage build)
# .dockerignore
```

---

## Validación Backend

### V1: Health Check Endpoint

```bash
# Health check (debe estar disponible incluso si BD no lo está)
curl -s http://localhost:5000/health | jq .

# Esperado:
# {
#   "status": "healthy",
#   "timestamp": "2026-06-24T10:30:00Z"
# }
```

**¿Qué significa?** Backend está corriendo y respondiendo a requests HTTP.

### V2: Compilación Local

```bash
cd backend

# Compilar proyecto
dotnet build -c Release

# Esperado: Build succeeded (sin errores)
```

**¿Qué significa?** Código backend compila correctamente; dependencias están resueltas.

### V3: Tests Unitarios

```bash
# Ejecutar tests backend
dotnet test --verbosity minimal

# Esperado: All tests passed (0 failed)
```

**¿Qué significa?** Suite de tests backend valida lógica core.

### V4: Verificar Capas Clean Architecture

```bash
# Verificar estructura de directorios
ls -la

# Esperado:
# Domain/
# Application/
# Infrastructure/
# Api/
# Tests/
```

```bash
# Verificar compilación de cada capa individual
dotnet build Domain/       # Sin referencia a Infrastructure
dotnet build Application/  # Importa Domain
dotnet build Infrastructure/  # Importa Domain, Application
dotnet build Api/          # Importa todos
```

**¿Qué significa?** Capas están correctamente separadas; dependencias van de afuera hacia adentro.

### V5: MongoDB Connection

```bash
# Ejecutar test simple de conexión
cat > test-mongo.cs << 'EOF'
using MongoDB.Driver;

var client = new MongoClient("mongodb://root:example_dev_password@localhost:27017/monolegal_dev");
var db = client.GetDatabase("monolegal_dev");
var collection = db.GetCollection<BsonDocument>("test");

await collection.InsertOneAsync(new BsonDocument { { "test", "document" } });
var doc = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();

Console.WriteLine(doc != null ? "✓ MongoDB connected" : "✗ Connection failed");
EOF

dotnet script test-mongo.cs 2>/dev/null || echo "Instalando dotnet-script..."
dotnet tool install -g dotnet-script
dotnet script test-mongo.cs
```

**¿Qué significa?** Backend puede conectar a MongoDB y ejecutar operaciones CRUD.

---

## Validación Frontend

### V1: Acceso Web

```bash
# Verificar que frontend está servido
curl -s http://localhost:5173 | head -20

# Esperado: HTML de React app (contiene <div id="root">)
```

O abrir en navegador: **http://localhost:5173**

Esperado: Página React cargada (no error 404)

### V2: Compilación Local

```bash
cd frontend

# Instalar dependencias
npm ci

# Build para producción
npm run build

# Esperado:
# ✓ built in 3.45s (o similar)
# dist/ creado con assets
```

### V3: Tests Unitarios

```bash
# Ejecutar tests frontend
npm run test -- --run

# Esperado: All tests passed (sin errores)
```

### V4: Linting & Formatting

```bash
# Verificar Biome compliance
npm run lint

# Esperado: No errors (0 issues)

# Verificar React Doctor
npm run lint:react

# Esperado: No warnings
```

### V5: TypeScript Strict Mode

```bash
# Verificar que tsconfig.json tiene strict: true
cat tsconfig.json | grep -A 5 '"compilerOptions"'

# Esperado: "strict": true presente
```

```bash
# Compilar TypeScript sin build
npx tsc --noEmit

# Esperado: 0 errors
```

---

## Validación MongoDB

### V1: Health Check

```bash
# Verificar que MongoDB está respondiendo
docker-compose exec mongo mongosh --eval "db.adminCommand('ping')"

# Esperado:
# { ok: 1 }
```

### V2: Verificar Persistencia

```bash
# Crear datos de prueba
docker-compose exec mongo mongosh << 'EOF'
use monolegal_dev
db.test_collection.insertOne({ name: "test", date: new Date() })
db.test_collection.find()
EOF

# Esperado: documento insertado y recuperado

# Detener MongoDB (pero no destruir volumen)
docker-compose stop mongo

# Esperar 5 segundos
sleep 5

# Reiniciar MongoDB
docker-compose start mongo

# Esperar 10 segundos para que levante
sleep 10

# Verificar que datos persisten
docker-compose exec mongo mongosh << 'EOF'
use monolegal_dev
db.test_collection.find()
EOF

# Esperado: documento AÚN está ahí (persistencia funciona)
```

### V3: Connection Pooling

```bash
# Verificar que conexión pool está activa
docker-compose exec backend dotnet << 'EOF'
using MongoDB.Driver;
var client = new MongoClient("mongodb://root:example_dev_password@mongo:27017");
var stats = client.Cluster.Description.Servers;
Console.WriteLine($"Connected servers: {stats.Count()}");
EOF
```

---

## Validación Integración Completa

### V1: Flujo Backend → MongoDB

```bash
# Crear script de integración
cat > integration-test.cs << 'EOF'
using MongoDB.Driver;

// 1. Connect
var client = new MongoClient("mongodb://root:example_dev_password@mongo:27017");
var db = client.GetDatabase("monolegal_dev");

// 2. Create collection
var col = db.GetCollection<BsonDocument>("invoices");

// 3. Insert
var doc = new BsonDocument { 
  { "_id", ObjectId.GenerateNewId() },
  { "client", "Test Client" },
  { "amount", 1000 },
  { "status", "pending" }
};
await col.InsertOneAsync(doc);

// 4. Query
var result = await col.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
Console.WriteLine($"✓ Inserted and found {result.Count} invoices");
EOF
```

### V2: Flujo Frontend → Backend

```bash
# Desde navegador en http://localhost:5173:
# 1. Abrir Dev Tools (F12)
# 2. Tab Network
# 3. Ejecutar en Console:
#    fetch('http://localhost:5000/health')
#      .then(r => r.json())
#      .then(d => console.log(d))

# Esperado:
# { status: "healthy", timestamp: "..." }
```

### V3: Docker Network

```bash
# Verificar que servicios se ven entre sí
docker-compose exec backend ping -c 2 mongo

# Esperado: PING mongo (...) - 2 packets transmitted, 2 received

docker-compose exec frontend ping -c 2 backend

# Esperado: PING backend (...) - 2 packets transmitted, 2 received
```

---

## Validación Performance

### P1: Backend Response Time

```bash
# Hacer request y medir tiempo
time curl -s http://localhost:5000/health > /dev/null

# Esperado: < 100ms (real: ~10-50ms)
```

### P2: Frontend Bundle Size

```bash
cd frontend
npm run build

# Ver tamaño de archivos
du -sh dist/
ls -lh dist/assets/

# Esperado: dist/ < 50MB (típicamente ~5-10MB gzipped)
```

### P3: Docker Image Size

```bash
# Ver tamaño de imágenes built
docker images | grep monolegal

# Esperado:
# monolegal-backend  < 500MB
# monolegal-frontend < 200MB
# monolegal-worker   < 500MB
```

---

## Troubleshooting

### ❌ "Connection refused" en localhost:5000

**Síntoma**: `curl: (7) Failed to connect to localhost port 5000`

**Solución**:
```bash
# 1. Verificar que backend está corriendo
docker-compose ps backend

# 2. Ver logs
docker-compose logs backend

# 3. Si Exited, reconstruir
docker-compose down
docker-compose up -d --build backend
```

### ❌ "MongoDB connection timeout"

**Síntoma**: Backend logs show `Error connecting to MongoDB`

**Solución**:
```bash
# 1. Verificar que MongoDB está healthy
docker-compose ps mongo

# 2. Verificar logs
docker-compose logs mongo

# 3. Si Unhealthy, esperar más tiempo:
sleep 30
docker-compose ps mongo

# 4. Si persiste, reiniciar
docker-compose restart mongo
sleep 10
```

### ❌ Port 5173/5000/27017 ya en uso

**Síntoma**: `Error: listen EADDRINUSE: address already in use :::5173`

**Solución**:
```bash
# Opción 1: Matar proceso que usa puerto
# macOS/Linux:
lsof -i :5173  # Ver qué usa puerto 5173
kill -9 <PID>

# Opción 2: Cambiar puerto en docker-compose.yml
# Cambiar:
# ports:
#   - "5174:5173"  # Usar 5174 en lugar de 5173
# y reconstruir
docker-compose up -d --build
```

### ❌ "ENOENT: no such file or directory" en npm

**Síntoma**: Frontend no levanta; logs show `ENOENT`

**Solución**:
```bash
cd frontend
rm -rf node_modules package-lock.json
npm ci  # Clean install
docker-compose up -d --build frontend
```

---

## Checklist de Validación Final

Antes de considerar la Fase 0 **COMPLETA**, verificar:

- [ ] ✅ `docker-compose ps` muestra 4 servicios como **Up (healthy)**
- [ ] ✅ `curl http://localhost:5000/health` retorna `{"status":"healthy"}`
- [ ] ✅ `curl http://localhost:5173` retorna HTML de React app
- [ ] ✅ Backend: `dotnet build` compila sin errores
- [ ] ✅ Frontend: `npm run build` genera `dist/` exitosamente
- [ ] ✅ MongoDB: `docker-compose exec mongo mongosh --eval "db.adminCommand('ping')"` retorna `{ ok: 1 }`
- [ ] ✅ Directorios existen: `backend/`, `frontend/`, `worker/`, `packages/shared/`
- [ ] ✅ Capas Clean Architecture separadas: `Domain/`, `Application/`, `Infrastructure/`, `Api/`
- [ ] ✅ TypeScript strict mode habilitado en `frontend/tsconfig.json`
- [ ] ✅ Docker images < 500MB: `docker images | grep monolegal`

---

## Próximos Pasos

Una vez validadas todas las verificaciones:

1. **Phase 1**: Generar tareas específicas con `/speckit.tasks`
2. **Phase 2**: Ejecutar implementación con `/speckit.implement`
3. **Phase 0.2**: Continuar con siguiente Spec (dependencias backend)

---

**Status**: ✅ **GUÍA DE VALIDACIÓN COMPLETA**

Para dudas durante validación, consultar:
- [Plan](plan.md) - Decisiones arquitectónicas
- [Research](research.md) - Justificación técnica de cada decisión
- [Data Model](data-model.md) - Estructura de componentes
- [Docker Compose Contract](contracts/docker-compose.yml) - Configuración exacta servicios
