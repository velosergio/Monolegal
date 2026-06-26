import { createContext, use, useEffect, useState } from 'react'

type Theme = 'dark' | 'light' | 'system'

interface ThemeProviderProps {
  children: React.ReactNode
  defaultTheme?: Theme
  storageKey?: string
}

interface ThemeProviderState {
  theme: Theme
  setTheme: (theme: Theme) => void
}

const initialState: ThemeProviderState = {
  theme: 'system',
  setTheme: () => null,
}

const ThemeProviderContext = createContext<ThemeProviderState>(initialState)

function applyThemeClass(theme: Theme) {
  const root = window.document.documentElement
  root.classList.remove('light', 'dark')

  if (theme === 'system') {
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
    root.classList.add(systemTheme)
    return
  }

  root.classList.add(theme)
}

export function ThemeProvider({
  children,
  defaultTheme = 'system',
  storageKey = 'ml-ui-theme',
  ...props
}: ThemeProviderProps) {
  const [theme, setTheme] = useState<Theme>(
    () => (localStorage.getItem(storageKey) as Theme) || defaultTheme
  )

  // Aplica el tema almacenado/por defecto al montar. Lee de localStorage en lugar
  // del estado `theme`, por lo que el efecto depende solo de props estables y no
  // arrastra un valor obsoleto en su clausura.
  useEffect(() => {
    applyThemeClass((localStorage.getItem(storageKey) as Theme) || defaultTheme)
  }, [storageKey, defaultTheme])

  const value: ThemeProviderState = {
    theme,
    setTheme: (newTheme: Theme) => {
      localStorage.setItem(storageKey, newTheme)
      applyThemeClass(newTheme)
      setTheme(newTheme)
    },
  }

  return (
    <ThemeProviderContext.Provider {...props} value={value}>
      {children}
    </ThemeProviderContext.Provider>
  )
}

/** Accede al tema actual y al setter del `ThemeProvider`. */
export function useTheme() {
  return use(ThemeProviderContext)
}
