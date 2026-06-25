import type React from 'react'
import './index.css'
import { InvoiceList } from './features/invoices/components/InvoiceList'
import { InvoiceTransitionsTab } from './features/settings/components/InvoiceTransitionsTab'

const App: React.FC = () => {
  return (
    <div className="app">
      <header className="app-header">
        <h1>MonoLegal</h1>
        <p>Gestión de Facturas</p>
      </header>
      <main className="app-main">
        <InvoiceList invoices={[]} />
        <InvoiceTransitionsTab />
      </main>
    </div>
  )
}

export default App
