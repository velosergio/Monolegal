import { cn } from '@/lib/utils'

const APP_VERSION = '0.1.0'

interface FooterProps {
  /** En modo colapsado muestra solo la versión, centrada. */
  collapsed?: boolean
}

/**
 * Pie del sidebar: nombre del producto, versión y año actual. Se ancla al fondo
 * de la navegación lateral (patrón de dashboard moderno) sobre la superficie de
 * marca oscura.
 */
export function Footer({ collapsed = false }: FooterProps) {
  const year = new Date().getFullYear()

  return (
    <footer
      className={cn(
        'border-t border-sidebar-border px-4 py-4 text-xs text-sidebar-foreground/60',
        collapsed && 'text-center'
      )}
    >
      {collapsed ? (
        <p>v{APP_VERSION}</p>
      ) : (
        <p>
          Monolegal · v{APP_VERSION} · © {year}
        </p>
      )}
    </footer>
  )
}
