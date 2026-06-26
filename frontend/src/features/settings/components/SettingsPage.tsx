import { type LucideIcon, Monitor, Moon, Sun } from 'lucide-react'
import { useTheme } from '@/components/theme-provider'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { cn } from '@/lib/utils'

type ThemeOption = 'light' | 'dark' | 'system'

const THEME_OPTIONS: ReadonlyArray<{ value: ThemeOption; label: string; icon: LucideIcon }> = [
  { value: 'system', label: 'Sistema', icon: Monitor },
  { value: 'light', label: 'Claro', icon: Sun },
  { value: 'dark', label: 'Oscuro', icon: Moon },
]

/**
 * Vista de Configuración. Por ahora alberga la preferencia de tema
 * (claro/oscuro/sistema); se irán añadiendo más secciones.
 */
export function SettingsPage() {
  useDocumentTitle('Configuración')
  const { theme, setTheme } = useTheme()

  return (
    <section aria-labelledby="settings-title" className="flex max-w-2xl flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 id="settings-title" className="font-heading text-2xl font-black tracking-tight">
          Configuración
        </h1>
        <p className="text-sm text-muted-foreground">Ajusta las preferencias del panel.</p>
      </header>

      <div className="rounded-lg border p-5">
        <div className="flex flex-col gap-1">
          <h2 className="font-heading text-base font-bold">Apariencia</h2>
          <p className="text-sm text-muted-foreground">
            Elige el tema de la interfaz. "Sistema" sigue la preferencia de tu dispositivo.
          </p>
        </div>

        <fieldset className="mt-4 min-w-0 border-0 p-0">
          <legend className="sr-only">Tema de la interfaz</legend>
          <div className="grid grid-cols-3 gap-2 sm:max-w-md">
            {THEME_OPTIONS.map(({ value, label, icon: Icon }) => {
              const selected = theme === value
              return (
                <button
                  key={value}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => setTheme(value)}
                  className={cn(
                    'flex flex-col items-center gap-2 rounded-[2px] border px-3 py-4 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                    selected
                      ? 'border-primary bg-primary/10 text-foreground'
                      : 'border-input text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                  )}
                >
                  <Icon className="h-5 w-5" aria-hidden="true" />
                  {label}
                </button>
              )
            })}
          </div>
        </fieldset>
      </div>
    </section>
  )
}
