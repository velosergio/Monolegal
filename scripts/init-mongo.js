// scripts/init-mongo.js
// Script de inicialización de MongoDB para docker-compose
// Se ejecuta automáticamente cuando mongo inicia (via ENTRYPOINT)

// Conectar a base de datos especificada
db = db.getSiblingDB('monolegal_dev');

// Crear colecciones con índices
db.createCollection('clientes');
db.clientes.createIndex({ nombre: 1 });
db.clientes.createIndex({ email: 1 }, { unique: true });

db.createCollection('facturas');
db.facturas.createIndex({ status: 1 });
db.facturas.createIndex({ clienteId: 1 });
db.facturas.createIndex({ fechaCreacion: -1 });
db.facturas.createIndex({ monto: 1 });

db.createCollection('plantillas');
db.plantillas.createIndex({ tipo: 1 });

db.createCollection('envios');
db.envios.createIndex({ facturaId: 1 });
db.envios.createIndex({ fechaEnvio: -1 });

db.createCollection('usuarios');
db.usuarios.createIndex({ email: 1 }, { unique: true });

db.createCollection('settings');

// Seed data: 3 clientes
db.clientes.insertMany([
  {
    _id: ObjectId(),
    nombre: "Acme Corp",
    email: "contacto@acme.com",
    telefonos: ["+1-555-0100"],
    direccion: "123 Business St, New York, NY 10001",
    rfc: "ACM000000001",
    activo: true,
    fechaCreacion: new Date("2026-01-01")
  },
  {
    _id: ObjectId(),
    nombre: "Tech Solutions Ltd",
    email: "ventas@techsolutions.com",
    telefonos: ["+1-555-0200"],
    direccion: "456 Innovation Ave, San Francisco, CA 94102",
    rfc: "TEC000000001",
    activo: true,
    fechaCreacion: new Date("2026-01-05")
  },
  {
    _id: ObjectId(),
    nombre: "Global Services Inc",
    email: "billing@globalservices.com",
    telefonos: ["+1-555-0300"],
    direccion: "789 Commerce Blvd, Chicago, IL 60601",
    rfc: "GLO000000001",
    activo: true,
    fechaCreacion: new Date("2026-01-10")
  }
]);

// Seed data: 8+ facturas en diferentes estados
const clientes = db.clientes.find({}).toArray();

db.facturas.insertMany([
  {
    _id: ObjectId(),
    clienteId: clientes[0]._id,
    numero: "2026-001",
    monto: 1500.00,
    moneda: "USD",
    status: "pagada",
    descripcion: "Servicios de consultoría - Enero",
    fechaCreacion: new Date("2026-01-15"),
    fechaVencimiento: new Date("2026-02-15"),
    fechaPago: new Date("2026-02-10"),
    ultimoRecordatorio: null,
    recordatorios: 0
  },
  {
    _id: ObjectId(),
    clienteId: clientes[0]._id,
    numero: "2026-002",
    monto: 2500.00,
    moneda: "USD",
    status: "primerrecordatorio",
    descripcion: "Servicios de desarrollo - Febrero",
    fechaCreacion: new Date("2026-02-01"),
    fechaVencimiento: new Date("2026-03-01"),
    ultimoRecordatorio: new Date("2026-03-10"),
    recordatorios: 1
  },
  {
    _id: ObjectId(),
    clienteId: clientes[1]._id,
    numero: "2026-003",
    monto: 3200.00,
    moneda: "USD",
    status: "segundorecordatorio",
    descripcion: "Licencias de software - Marzo",
    fechaCreacion: new Date("2026-03-01"),
    fechaVencimiento: new Date("2026-04-01"),
    ultimoRecordatorio: new Date("2026-04-15"),
    recordatorios: 2
  },
  {
    _id: ObjectId(),
    clienteId: clientes[1]._id,
    numero: "2026-004",
    monto: 1800.00,
    moneda: "USD",
    status: "primerrecordatorio",
    descripcion: "Mantenimiento trimestral - Abril",
    fechaCreacion: new Date("2026-04-01"),
    fechaVencimiento: new Date("2026-05-01"),
    ultimoRecordatorio: new Date("2026-05-05"),
    recordatorios: 1
  },
  {
    _id: ObjectId(),
    clienteId: clientes[2]._id,
    numero: "2026-005",
    monto: 4100.00,
    moneda: "USD",
    status: "pagada",
    descripcion: "Implementación de sistema - Mayo",
    fechaCreacion: new Date("2026-05-01"),
    fechaVencimiento: new Date("2026-06-01"),
    fechaPago: new Date("2026-06-01"),
    ultimoRecordatorio: null,
    recordatorios: 0
  },
  {
    _id: ObjectId(),
    clienteId: clientes[2]._id,
    numero: "2026-006",
    monto: 2200.00,
    moneda: "USD",
    status: "desactivada",
    descripcion: "Soporte técnico adicional - Junio",
    fechaCreacion: new Date("2026-06-01"),
    fechaVencimiento: new Date("2026-07-01"),
    ultimoRecordatorio: new Date("2026-08-01"),
    recordatorios: 3,
    razonDesactivacion: "Cuenta cancelada por cliente"
  },
  {
    _id: ObjectId(),
    clienteId: clientes[0]._id,
    numero: "2026-007",
    monto: 950.00,
    moneda: "USD",
    status: "primerrecordatorio",
    descripcion: "Servicio de auditoría - Julio",
    fechaCreacion: new Date("2026-07-01"),
    fechaVencimiento: new Date("2026-08-01"),
    ultimoRecordatorio: new Date("2026-08-20"),
    recordatorios: 1
  },
  {
    _id: ObjectId(),
    clienteId: clientes[1]._id,
    numero: "2026-008",
    monto: 3600.00,
    moneda: "USD",
    status: "segundorecordatorio",
    descripcion: "Capacitación de personal - Agosto",
    fechaCreacion: new Date("2026-08-01"),
    fechaVencimiento: new Date("2026-09-01"),
    ultimoRecordatorio: new Date("2026-09-25"),
    recordatorios: 2
  }
]);

print("✅ MongoDB inicializado exitosamente");
print("Clientes creados: " + db.clientes.countDocuments());
print("Facturas creadas: " + db.facturas.countDocuments());
