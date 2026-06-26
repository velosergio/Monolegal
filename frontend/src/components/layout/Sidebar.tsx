import { ChevronLeft, ChevronRight } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { Footer } from './Footer'
import { NAV_ITEMS } from './navigation'

interface SidebarProps {
  /** Colapsa el sidebar a solo iconos (solo escritorio). */
  collapsed?: boolean
  /** Alterna el estado colapsado; si se omite, no se muestra el botón. */
  onToggleCollapse?: () => void
  /** Se invoca al activar un ítem navegable (cierra el menú móvil). */
  onNavigate?: () => void
}

/**
 * Sidebar de marca a altura completa (patrón de dashboard moderno): cabecera con
 * el logo oficial y el botón de colapsar/expandir, navegación principal por
 * rutas (react-router) que ocupa el espacio disponible y el pie anclado al
 * fondo. "Dashboard" se muestra deshabilitado con la etiqueta "Próximamente".
 */
export function Sidebar({ collapsed = false, onToggleCollapse, onNavigate }: SidebarProps) {
  return (
    <div className="flex h-full flex-col text-sidebar-foreground">
      <div
        className={cn(
          'flex h-16 shrink-0 items-center border-b border-sidebar-border',
          collapsed ? 'justify-center px-2' : 'justify-between px-5'
        )}
      >
        {!collapsed && <img src="/logo.png" alt="Monolegal" className="h-7 w-auto" />}
        {onToggleCollapse && (
          <button
            type="button"
            onClick={onToggleCollapse}
            aria-label={collapsed ? 'Expandir menú lateral' : 'Colapsar menú lateral'}
            className="flex h-8 w-8 items-center justify-center rounded-[2px] text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-active"
          >
            {collapsed ? (
              <ChevronRight className="h-4 w-4" aria-hidden="true" />
            ) : (
              <ChevronLeft className="h-4 w-4" aria-hidden="true" />
            )}
          </button>
        )}
      </div>

      <nav
        id="sidebar-navigation"
        aria-label="Navegación principal"
        className="flex flex-1 flex-col gap-1 overflow-y-auto p-3"
      >
        {NAV_ITEMS.map(({ to, label, icon: Icon, disabled }) => {
          if (disabled) {
            return (
              <button
                key={to}
                type="button"
                disabled
                aria-disabled="true"
                title={collapsed ? `${label} · Próximamente` : undefined}
                className={cn(
                  'flex cursor-not-allowed items-center gap-3 rounded-md px-3 py-2 text-left text-sm font-medium text-sidebar-foreground/40',
                  collapsed ? 'justify-center' : 'justify-between'
                )}
              >
                <span className="flex items-center gap-3">
                  <Icon className="h-4 w-4 shrink-0" aria-hidden="true" />
                  {!collapsed && label}
                </span>
                {!collapsed && (
                  <span className="rounded-[2px] bg-sidebar-accent px-2 py-0.5 text-[10px] uppercase tracking-wide">
                    Próximamente
                  </span>
                )}
              </button>
            )
          }

          return (
            <NavLink
              key={to}
              to={to}
              onClick={onNavigate}
              title={collapsed ? label : undefined}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-md border-l-2 px-3 py-2 text-left text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-active',
                  collapsed && 'justify-center',
                  isActive
                    ? 'border-sidebar-active bg-sidebar-active/15 text-sidebar-active'
                    : 'border-transparent text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-foreground'
                )
              }
            >
              <Icon className="h-4 w-4 shrink-0" aria-hidden="true" />
              {!collapsed && label}
            </NavLink>
          )
        })}
      </nav>

      <Footer collapsed={collapsed} />
    </div>
  )
}
