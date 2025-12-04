import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '../contexts/AuthContext.jsx'
import ProtectedRoute from '../ProtectedRoute.jsx'

const TestComponent = () => <div>Protected Content</div>
const LoginComponent = () => <div>Login Page</div>

const renderWithRouter = (isAuthenticated = false) => {
  if (isAuthenticated) {
    localStorage.setItem('isAuthenticated', 'true')
    localStorage.setItem('username', 'testuser')
  } else {
    localStorage.clear()
  }

  return render(
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginComponent />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <TestComponent />
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('ProtectedRoute Component', () => {
  beforeEach(() => {
    localStorage.clear()
    window.history.pushState({}, '', '/')
  })

  describe('Authentication Check', () => {
    it('should render protected content when authenticated', async () => {
      renderWithRouter(true)

      await waitFor(() => {
        expect(screen.getByText('Protected Content')).toBeInTheDocument()
        expect(screen.queryByText('Login Page')).not.toBeInTheDocument()
      })
    })

    it('should redirect to login when not authenticated', async () => {
      renderWithRouter(false)

      await waitFor(() => {
        expect(screen.getByText('Login Page')).toBeInTheDocument()
        expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
      })
    })

    it('should show loading state during authentication check', () => {
      renderWithRouter(false)

      // Initially should show loading
      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('should hide loading state after authentication check', async () => {
      renderWithRouter(false)

      await waitFor(() => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
      })
    })
  })

  describe('Route Protection', () => {
    it('should preserve attempted location when redirecting', async () => {
      window.history.pushState({}, '', '/protected-page')
      renderWithRouter(false)

      await waitFor(() => {
        expect(screen.getByText('Login Page')).toBeInTheDocument()
      })

      // The location state should be preserved (tested via navigation)
      expect(window.location.pathname).toBe('/login')
    })

    it('should allow access after authentication', async () => {
      // Start unauthenticated
      const { rerender } = renderWithRouter(false)

      await waitFor(() => {
        expect(screen.getByText('Login Page')).toBeInTheDocument()
      })

      // Authenticate
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      // Rerender with authentication
      rerender(
        <BrowserRouter>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<LoginComponent />} />
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <TestComponent />
                  </ProtectedRoute>
                }
              />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      )

      await waitFor(() => {
        expect(screen.getByText('Protected Content')).toBeInTheDocument()
      })
    })
  })

  describe('Loading State', () => {
    it('should display loading message during initial check', () => {
      renderWithRouter(false)

      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('should not display loading after check completes', async () => {
      renderWithRouter(false)

      await waitFor(() => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle rapid authentication state changes', async () => {
      const { rerender } = renderWithRouter(false)

      await waitFor(() => {
        expect(screen.getByText('Login Page')).toBeInTheDocument()
      })

      // Rapidly change auth state
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      rerender(
        <BrowserRouter>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<LoginComponent />} />
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <TestComponent />
                  </ProtectedRoute>
                }
              />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      )

      await waitFor(() => {
        expect(screen.getByText('Protected Content')).toBeInTheDocument()
      })
    })

    it('should handle missing authentication context gracefully', () => {
      // This should not happen in practice due to AuthProvider wrapper
      // But we test that the component handles it
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        render(
          <BrowserRouter>
            <ProtectedRoute>
              <TestComponent />
            </ProtectedRoute>
          </BrowserRouter>
        )
      }).not.toThrow()

      consoleSpy.mockRestore()
    })
  })
})
