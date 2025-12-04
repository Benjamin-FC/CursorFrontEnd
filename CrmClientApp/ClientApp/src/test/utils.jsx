import { render } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../contexts/AuthContext.jsx'

/**
 * Custom render function that includes common providers
 */
export function renderWithProviders(ui, { route = '/', ...options } = {}) {
  // Set the initial route
  window.history.pushState({}, 'Test page', route)

  const Wrapper = ({ children }) => {
    return (
      <BrowserRouter>
        <AuthProvider>
          {children}
        </AuthProvider>
      </BrowserRouter>
    )
  }

  return render(ui, { wrapper: Wrapper, ...options })
}

/**
 * Create a mock fetch response
 */
export function createMockResponse(data, status = 200, ok = true) {
  return {
    ok,
    status,
    json: async () => data,
    text: async () => JSON.stringify(data),
    headers: new Headers(),
  }
}

/**
 * Mock fetch with a response
 */
export function mockFetch(response) {
  global.fetch = vi.fn(() => Promise.resolve(response))
}

/**
 * Wait for async operations to complete
 */
export async function waitForAsync() {
  await new Promise(resolve => setTimeout(resolve, 0))
}
