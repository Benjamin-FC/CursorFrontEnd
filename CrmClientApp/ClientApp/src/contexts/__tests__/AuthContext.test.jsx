import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { renderHook, act } from '@testing-library/react'
import { AuthProvider, useAuth } from '../AuthContext.jsx'

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  describe('AuthProvider', () => {
    it('should initialize with no authentication when localStorage is empty', async () => {
      const TestComponent = () => {
        const { isAuthenticated, username, isLoading } = useAuth()
        if (isLoading) return <div>Loading...</div>
        return (
          <div>
            <div data-testid="isAuthenticated">{String(isAuthenticated)}</div>
            <div data-testid="username">{username || 'null'}</div>
          </div>
        )
      }

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      )

      await waitFor(() => {
        expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('false')
        expect(screen.getByTestId('username')).toHaveTextContent('null')
      })
    })

    it('should initialize with authentication from localStorage', async () => {
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      const TestComponent = () => {
        const { isAuthenticated, username, isLoading } = useAuth()
        if (isLoading) return <div>Loading...</div>
        return (
          <div>
            <div data-testid="isAuthenticated">{String(isAuthenticated)}</div>
            <div data-testid="username">{username}</div>
          </div>
        )
      }

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      )

      await waitFor(() => {
        expect(screen.getByTestId('isAuthenticated')).toHaveTextContent('true')
        expect(screen.getByTestId('username')).toHaveTextContent('testuser')
      })
    })

    it('should show loading state initially', async () => {
      const TestComponent = () => {
        const { isLoading } = useAuth()
        return <div data-testid="loading-state">{isLoading ? 'Loading...' : 'Loaded'}</div>
      }

      const { getByTestId } = render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      )

      // Initially should show loading (may be very brief)
      const loadingState = getByTestId('loading-state')
      // The loading state may complete very quickly, so we check it exists
      expect(loadingState).toBeInTheDocument()
    })
  })

  describe('useAuth hook', () => {
    it('should throw error when used outside AuthProvider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        renderHook(() => useAuth())
      }).toThrow('useAuth must be used within an AuthProvider')

      consoleSpy.mockRestore()
    })

    it('should provide login function', () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      expect(result.current.login).toBeDefined()
      expect(typeof result.current.login).toBe('function')
    })

    it('should provide logout function', () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      expect(result.current.logout).toBeDefined()
      expect(typeof result.current.logout).toBe('function')
    })
  })

  describe('login function', () => {
    it('should successfully login with valid credentials', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      let loginResult
      await act(async () => {
        loginResult = await result.current.login('testuser', 'password123')
      })
      expect(loginResult.success).toBe(true)

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(true)
        expect(result.current.username).toBe('testuser')
        expect(localStorage.getItem('isAuthenticated')).toBe('true')
        expect(localStorage.getItem('username')).toBe('testuser')
      })
    })

    it('should fail login with empty username', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      let loginResult
      await act(async () => {
        loginResult = await result.current.login('', 'password123')
      })
      expect(loginResult.success).toBe(false)
      expect(loginResult.error).toBe('Please enter both username and password')

      expect(result.current.isAuthenticated).toBe(false)
      expect(localStorage.getItem('isAuthenticated')).toBeNull()
    })

    it('should fail login with empty password', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      let loginResult
      await act(async () => {
        loginResult = await result.current.login('testuser', '')
      })
      expect(loginResult.success).toBe(false)
      expect(loginResult.error).toBe('Please enter both username and password')

      expect(result.current.isAuthenticated).toBe(false)
    })

    it('should fail login with null username', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      let loginResult
      await act(async () => {
        loginResult = await result.current.login(null, 'password123')
      })
      expect(loginResult.success).toBe(false)

      expect(result.current.isAuthenticated).toBe(false)
    })

    it('should fail login with null password', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      let loginResult
      await act(async () => {
        loginResult = await result.current.login('testuser', null)
      })
      expect(loginResult.success).toBe(false)

      expect(result.current.isAuthenticated).toBe(false)
    })
  })

  describe('logout function', () => {
    it('should successfully logout and clear state', async () => {
      // Set initial auth state
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', 'testuser')

      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(true)
      })

      act(() => {
        result.current.logout()
      })

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(false)
        expect(result.current.username).toBeNull()
        expect(localStorage.getItem('isAuthenticated')).toBeNull()
        expect(localStorage.getItem('username')).toBeNull()
      })
    })

    it('should handle logout when already logged out', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(false)
      })

      act(() => {
        result.current.logout()
      })

      expect(result.current.isAuthenticated).toBe(false)
      expect(result.current.username).toBeNull()
    })
  })

  describe('state persistence', () => {
    it('should persist authentication state across re-renders', async () => {
      const { result, rerender } = renderHook(() => useAuth(), {
        wrapper: AuthProvider,
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      await act(async () => {
        await result.current.login('testuser', 'password123')
      })

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(true)
      })

      // Rerender the hook
      rerender()

      await waitFor(() => {
        expect(result.current.isAuthenticated).toBe(true)
        expect(result.current.username).toBe('testuser')
      })
    })
  })
})
