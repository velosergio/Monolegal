import type React from 'react'
import './index.css'

const App: React.FC = () => {
  return (
    <div className="app">
      <header className="app-header">
        <h1>MonoLegal</h1>
        <p>Gestión de Facturas</p>
      </header>
      <main className="app-main">{/* Main content will be rendered here */}</main>
    </div>
  )
}

export default App
