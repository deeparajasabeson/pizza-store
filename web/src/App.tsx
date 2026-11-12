import type { ReactElement } from 'react'

// App shell only. Routes (/menu, /cart, /checkout, /orders, ...) are added per feature
// under src/features/ and wired up in src/routes/.
export default function App(): ReactElement {
  return (
    <div className="min-h-screen bg-white text-gray-900">
      <header className="border-b border-gray-200 px-4 py-3">
        <h1 className="text-xl font-semibold">Pizza Store</h1>
      </header>
      <main id="main" className="px-4 py-6">
        <p>Online ordering is coming soon.</p>
      </main>
    </div>
  )
}
