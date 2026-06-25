# Quickstart — Verificación de Conexión MongoDB

**Feature**: `004-mongodb-connection` | **Plan**: [plan.md](plan.md)

Guía de validación end-to-end que prueba los criterios GIVEN/WHEN/THEN de la spec 0.4. No contiene código de implementación; los detalles viven en [contracts/](contracts/) y [data-model.md](data-model.md), y la implementación en `tasks.md`.

## Prerrequisitos

- Docker + Docker Compose instalados.
- `.NET 10 SDK` para ejecutar los tests del backend.
- Archivo `.env` creado a partir de `.env.example` (define `MONGODB_URI`).

---

## Escenario 1 — Servicio MongoDB corriendo en 27017 (HU1)

```bash
docker-compose up -d mongo
docker-compose ps mongo                      # estado: running / healthy
docker-compose exec mongo mongosh --eval "db.runCommand({ ping: 1 })"
```

**Esperado**: el servicio `monolegal-mongo` figura `healthy`; el `ping` devuelve `{ ok: 1 }`. El puerto 27017 acepta conexiones desde el host.

---

## Escenario 2 — Base `monolegal_dev` disponible (HU2)

```bash
docker-compose exec mongo mongosh monolegal_dev --eval "db.getName(); db.getCollectionNames()"
```

**Esperado**: `db.getName()` → `monolegal_dev`; las colecciones inicializadas por `init-mongo.js` (`clientes`, `facturas`, `plantillas`, `envios`, `usuarios`, `settings`) están presentes. La base está lista sin pasos manuales (FR-003, SC-002).

---

## Escenario 3 — Conexión verificada desde el backend (HU3)

```bash
docker-compose up -d backend
docker-compose logs backend | grep -i "mongo"        # log estructurado de verificación
curl -i http://localhost:5000/health                  # 200 Healthy
```

**Esperado**:
- Los logs del backend contienen el registro estructurado de **conexión exitosa** a `monolegal_dev` (host, base, duración) — FR-005/FR-006.
- `GET /health` → `200 Healthy` (el check `mongodb` ejecutó `ping`) — FR-008, contrato [health-endpoint.md](contracts/health-endpoint.md).

---

## Escenario 4 — Fallo de conectividad reportado con claridad (FR-007, casos límite)

Simular indisponibilidad (detener Mongo con el backend arriba):

```bash
docker-compose stop mongo
curl -i http://localhost:5000/health                  # 503 Unhealthy
docker-compose logs backend | tail                    # error clasificado "servicio no disponible"
```

Simular credenciales incorrectas (URI con password inválido):

```bash
# con MONGODB_URI apuntando a credenciales inválidas
docker-compose logs backend | grep -i "auth"          # error clasificado "autenticación"
```

**Esperado**: `/health` → `503 Unhealthy`; los logs distinguen "servicio no disponible" de "autenticación" (FR-007, SC-004). Sin fallo silencioso.

---

## Escenario 5 — Persistencia entre reinicios (FR-009)

```bash
docker-compose exec mongo mongosh monolegal_dev --eval "db.settings.insertOne({ _id: 'qs', v: 1 })"
docker-compose down            # sin -v: conserva volúmenes
docker-compose up -d mongo
docker-compose exec mongo mongosh monolegal_dev --eval "db.settings.findOne({ _id: 'qs' })"
```

**Esperado**: el documento insertado sigue presente tras el reinicio (volúmenes `mongo_data`/`mongo_config`) — SC-005.

---

## Validación automatizada (Test-First)

```bash
cd backend
dotnet test --filter "Category=Integration"   # conexión/ping, health check (requiere Mongo arriba)
dotnet test --filter "Category!=Integration"  # binding de MongoDbOptions, clasificación de error
```

**Esperado**: todas las suites en verde. Cobertura de los contratos de conexión y del health check (constitución, Principio IV).

---

## Checklist de éxito (resumen de Criterios de Éxito)

- [ ] SC-001: `docker-compose up` deja Mongo activo y 27017 accesible.
- [ ] SC-002: `monolegal_dev` disponible sin pasos manuales.
- [ ] SC-003: backend confirma conexión < 10s con log observable.
- [ ] SC-004: fallo simulado → mensaje claro y diferenciable.
- [ ] SC-005: datos persisten tras reinicio.
- [ ] SC-006: clonar + `docker-compose up` deja la base conectada y verificada.
- [ ] SC-007: 0 credenciales hardcodeadas; todo desde `MONGODB_URI`.
