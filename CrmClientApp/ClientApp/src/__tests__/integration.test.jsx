import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '../contexts/AuthContext.jsx'
import Login from '../Login.jsx'
import App from '../App.jsx'
import ProtectedRoute from '../ProtectedRoute.jsx'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const renderApp = () => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <App />
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('Integration Tests', () => {
  beforeEach(() => {
    localStorage.clear()
    mockNavigate.mockClear()
    global.fetch = vi.fn()
    window.history.pushState({}, '', '/login')
  })

  describe('Full Authentication Flow', () => {
    it('should complete full login and access protected route', async () => {
      const user = userEvent.setup()
      renderApp()

      // Start on login page
      expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()

      // Fill in login form
      const usernameInput = screen.getByLabelText(/username/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const loginButton = screen.getByRole('button', { name: /sign in/i })

      await user.type(usernameInput, 'testuser')
      await user.type(passwordInput, 'password123')
      await user.click(loginButton)

      // Should navigate to main app
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
      })

      // Verify authentication state
      expect(localStorage.getItem('isAuthenticated')).toBe('true')
      expect(localStorage.getItem('username')).toBe('testuser')
    })

    it('should redirect to login when accessing protected route unauthenticated', async () => {
      window.history.pushState({}, '', '/')
      renderApp()

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()
      })
    })
  })

  describe('Search Flow After Login', () => {
    it('should allow searching after successful login', async () => {
      const user = userEvent.setup()
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      window.history.pushState({}, '', '/')
      renderApp()

      // Should be on main app
      await waitFor(() => {
        expect(screen.getByText('Client Data Search')).toBeInTheDocument()
      })

      // Perform search
      const mockData = { data: { id: '12345', name: 'Test Client' } }
      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
      })
    })
  })

  describe('Logout Flow', () => {
    it('should logout and redirect to login page', async () => {
      const user = userEvent.setup()
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      window.history.pushState({}, '', '/')
      renderApp()

      await waitFor(() => {
        expect(screen.getByText('Client Data Search')).toBeInTheDocument()
      })

      const logoutButton = screen.getByRole('button', { name: /logout/i })
      await user.click(logoutButton)

      expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
      expect(localStorage.getItem('isAuthenticated')).toBeNull()
    })
  })

  describe('Error Recovery', () => {
    it('should recover from API error and allow retry', async () => {
      const user = userEvent.setup()
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      window.history.pushState({}, '', '/')
      renderApp()

      await waitFor(() => {
        expect(screen.getByText('Client Data Search')).toBeInTheDocument()
      })

      // First attempt - error
      global.fetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Server error' }),
      })

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText(/error/i)).toBeInTheDocument()
      })

      // Second attempt - success
      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: { id: '12345' } }),
      })

      await user.clear(input)
      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
        expect(screen.queryByText(/error/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('State Persistence', () => {
    it('should maintain authentication state across page refreshes', async () => {
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      window.history.pushState({}, '', '/')
      renderApp()

      await waitFor(() => {
        expect(screen.getByText('Client Data Search')).toBeInTheDocument()
        expect(screen.getByText(/welcome, testuser/i)).toBeInTheDocument()
      })
    })
  })

  describe('Navigation Flow', () => {
    it('should handle navigation between routes correctly', async () => {
      const user = userEvent.setup()

      // Start unauthenticated
      window.history.pushState({}, '', '/')
      renderApp()

      // Should redirect to login
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()
      })

      // Login
      const usernameInput = screen.getByLabelText(/username/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const loginButton = screen.getByRole('button', { name: /sign in/i })

      await user.type(usernameInput, 'testuser')
      await user.type(passwordInput, 'password123')
      await user.click(loginButton)

      // Should navigate to main app
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
      })
    })
  })
})
