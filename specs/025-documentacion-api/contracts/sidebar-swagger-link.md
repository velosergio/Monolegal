# Contrato — Ítem de Swagger UI en el sidebar

## Modelo de navegación (`frontend/src/components/layout/navigation.ts`)

Se extiende `NavItem` para soportar **enlaces externos** sin romper los ítems de ruta existentes:

- Ítems de ruta (actuales): `{ to, label, icon, disabled }` → renderizados con `NavLink`.
- Ítem externo (nuevo): incluye `href` (URL externa) y un marcador `external: true` → renderizado con `<a>`.

Se añade el ítem de Swagger, por ejemplo:

```ts
{ label: 'API (Swagger)', icon: <IconoApropiado>, external: true, href: SWAGGER_URL }
```

donde `SWAGGER_URL = import.meta.env.VITE_SWAGGER_URL ?? '/swagger'`.

## Render (`Sidebar.tsx`)

- Para un ítem externo, renderiza:
  `<a href={href} target="_blank" rel="noopener noreferrer">` con el mismo estilo visual que los ítems navegables (icono + etiqueta; en estado colapsado solo icono con `title`).
- No participa del resaltado de "activo" de `NavLink` (es un enlace externo).
- Respeta el comportamiento colapsado/expandido y la accesibilidad (texto o `aria-label`/`title`).

## Configuración de entorno

- `frontend/.env.example`: documenta `VITE_SWAGGER_URL` (default `/swagger`).
- `frontend/vite.config.ts`: el proxy de dev reenvía además `/swagger` y `/openapi` al backend (igual que `/api`), para que el valor por defecto `/swagger` funcione en el mismo origen en desarrollo.
- **Degradación elegante**: si `VITE_SWAGGER_URL` está vacío (p. ej. producción con Swagger deshabilitado, Spec 010 D3), el ítem **no se muestra**, evitando un enlace roto.

## Criterios de aceptación

| ID | Criterio | FR |
|----|----------|----|
| AC-1 | El sidebar muestra un ítem identificado como acceso a la documentación de la API (Swagger) cuando hay URL configurada | FR-007 |
| AC-2 | Activar el ítem abre Swagger UI (URL configurada) en una pestaña nueva con `rel="noopener noreferrer"` | FR-008 |
| AC-3 | La URL proviene de `VITE_SWAGGER_URL` (default `/swagger`), sin host fijo embebido | FR-009 |
| AC-4 | En estado colapsado y expandido el acceso sigue siendo alcanzable y coherente con los demás ítems | FR-007 |
| AC-5 | Si la URL está vacía, el ítem se oculta (sin enlace roto) | FR-009 |

## Pruebas (Vitest + Testing Library)

- Renderiza el sidebar y verifica que existe el enlace de Swagger con `href` esperado, `target="_blank"` y `rel` seguro (AC-1..AC-3).
- Verifica que con `VITE_SWAGGER_URL` vacío el ítem no se renderiza (AC-5).
- No se altera el comportamiento de los ítems de ruta existentes.
