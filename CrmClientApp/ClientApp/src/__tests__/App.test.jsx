import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../contexts/AuthContext.jsx'
import App from '../App.jsx'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const renderApp = (initialAuth = false) => {
  if (initialAuth) {
    localStorage.setItem('isAuthenticated', 'true')
    localStorage.setItem('username', 'testuser')
  }

  return render(
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('App Component', () => {
  beforeEach(() => {
    localStorage.clear()
    mockNavigate.mockClear()
    global.fetch = vi.fn()
  })

  describe('Rendering', () => {
    it('should render header with title', () => {
      renderApp(true)

      expect(screen.getByText('CRM Client Data Fetcher')).toBeInTheDocument()
    })

    it('should render welcome message with username', () => {
      renderApp(true)

      expect(screen.getByText(/welcome, testuser/i)).toBeInTheDocument()
    })

    it('should render logout button', () => {
      renderApp(true)

      expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument()
    })

    it('should render search form', () => {
      renderApp(true)

      expect(screen.getByText('Client Data Search')).toBeInTheDocument()
      expect(screen.getByLabelText(/client id/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument()
    })

    it('should render search input with icon', () => {
      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      expect(input).toBeInTheDocument()
      expect(input).toHaveAttribute('placeholder', 'Enter client ID to search')
    })

    it('should have search button disabled initially', () => {
      renderApp(true)

      const searchButton = screen.getByRole('button', { name: /search/i })
      expect(searchButton).toBeDisabled()
    })
  })

  describe('Search Functionality', () => {
    it('should enable search button when client ID is entered', async () => {
      const user = userEvent.setup()
      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      expect(searchButton).toBeDisabled()

      await user.type(input, '12345')

      expect(searchButton).not.toBeDisabled()
    })

    it('should keep search button disabled with whitespace only', async () => {
      const user = userEvent.setup()
      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '   ')

      expect(searchButton).toBeDisabled()
    })

    it('should fetch client data on form submission', async () => {
      const user = userEvent.setup()
      const mockData = { data: { id: '12345', name: 'Test Client' } }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/Crm/GetClientData?id=12345',
        expect.objectContaining({
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        })
      )
    })

    it('should display loading state during search', async () => {
      const user = userEvent.setup()
      let resolveFetch
      const fetchPromise = new Promise(resolve => {
        resolveFetch = resolve
      })

      global.fetch.mockReturnValueOnce(fetchPromise)

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Searching...')).toBeInTheDocument()
        expect(input).toBeDisabled()
      })

      // Resolve the fetch promise inside act() to avoid warnings
      await act(async () => {
        resolveFetch({
          ok: true,
          json: async () => ({ data: {} }),
        })
        // Wait a bit for the promise to resolve
        await new Promise(resolve => setTimeout(resolve, 10))
      })
    })

    it('should display client data after successful fetch', async () => {
      const user = userEvent.setup()
      const mockData = {
        data: {
          id: '12345',
          name: 'Test Client',
          email: 'test@example.com',
        },
      }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
        // Use more specific queries to avoid matching "Client ID" label or subtitle
        const resultsSection = screen.getByText('Client Data').closest('.results-section')
        expect(resultsSection).toBeInTheDocument()
        // Check for data keys within the results section
        expect(screen.getByText('id:')).toBeInTheDocument()
        expect(screen.getByText('name:')).toBeInTheDocument()
        expect(screen.getByText('email:')).toBeInTheDocument()
      })
    })

    it('should parse JSON string data', async () => {
      const user = userEvent.setup()
      const jsonString = JSON.stringify({ id: '12345', name: 'Test' })
      const mockData = { data: jsonString }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
      })
    })

    it('should display string data as-is when not valid JSON', async () => {
      const user = userEvent.setup()
      const mockData = { data: 'Plain text response' }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Plain text response')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('should display error message on API error', async () => {
      const user = userEvent.setup()
      global.fetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Client not found' }),
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client not found')).toBeInTheDocument()
      })
    })

    it('should display generic error on network failure', async () => {
      const user = userEvent.setup()
      global.fetch.mockRejectedValueOnce(new Error('Network error'))

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText(/error/i)).toBeInTheDocument()
      })
    })

    it('should clear error when new search is initiated', async () => {
      const user = userEvent.setup()

      // First, trigger an error
      global.fetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Error message' }),
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Error message')).toBeInTheDocument()
      })

      // Then, trigger a successful search
      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: { id: '12345' } }),
      })

      await user.clear(input)
      await user.type(input, '67890')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.queryByText('Error message')).not.toBeInTheDocument()
      })
    })
  })

  describe('Results Display', () => {
    it('should display results section when data is available', async () => {
      const user = userEvent.setup()
      const mockData = { data: { id: '12345' } }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
        expect(screen.getByRole('button', { name: /clear results/i })).toBeInTheDocument()
      })
    })

    it('should clear results when clear button is clicked', async () => {
      const user = userEvent.setup()
      const mockData = { data: { id: '12345' } }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText('Client Data')).toBeInTheDocument()
      })

      const clearButton = screen.getByRole('button', { name: /clear results/i })
      await user.click(clearButton)

      await waitFor(() => {
        expect(screen.queryByText('Client Data')).not.toBeInTheDocument()
      })
    })

    it('should format object data as key-value pairs', async () => {
      const user = userEvent.setup()
      const mockData = {
        data: {
          id: '12345',
          name: 'Test Client',
          status: 'Active',
        },
      }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText(/id/i)).toBeInTheDocument()
        expect(screen.getByText(/name/i)).toBeInTheDocument()
        expect(screen.getByText(/status/i)).toBeInTheDocument()
        expect(screen.getByText('12345')).toBeInTheDocument()
        expect(screen.getByText('Test Client')).toBeInTheDocument()
        expect(screen.getByText('Active')).toBeInTheDocument()
      })
    })

    it('should handle nested object data', async () => {
      const user = userEvent.setup()
      const mockData = {
        data: {
          id: '12345',
          contact: {
            email: 'test@example.com',
            phone: '123-456-7890',
          },
        },
      }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        expect(screen.getByText(/contact/i)).toBeInTheDocument()
        // Nested objects should be stringified
        expect(screen.getByText(/"email"/)).toBeInTheDocument()
      })
    })
  })

  describe('Logout Functionality', () => {
    it('should logout and redirect to login page', async () => {
      const user = userEvent.setup()
      renderApp(true)

      const logoutButton = screen.getByRole('button', { name: /logout/i })
      await user.click(logoutButton)

      expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
      expect(localStorage.getItem('isAuthenticated')).toBeNull()
    })
  })

  describe('Form Validation', () => {
    it('should require client ID input', () => {
      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      expect(input).toBeRequired()
    })

    it('should prevent submission with empty client ID', async () => {
      const user = userEvent.setup()
      renderApp(true)

      const searchButton = screen.getByRole('button', { name: /search/i })
      
      // Button should be disabled when input is empty
      expect(searchButton).toBeDisabled()
      
      // Try to click the disabled button (should not trigger submission)
      await user.click(searchButton)

      // Form validation should prevent submission
      expect(global.fetch).not.toHaveBeenCalled()
    })
  })

  describe('Accessibility', () => {
    it('should have proper label for client ID input', () => {
      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      expect(input).toHaveAttribute('id', 'clientId')
    })

    it('should have aria-label for clear button', async () => {
      const user = userEvent.setup()
      const mockData = { data: { id: '12345' } }

      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      renderApp(true)

      const input = screen.getByLabelText(/client id/i)
      const searchButton = screen.getByRole('button', { name: /search/i })

      await user.type(input, '12345')
      await user.click(searchButton)

      await waitFor(() => {
        const clearButton = screen.getByRole('button', { name: /clear results/i })
        expect(clearButton).toHaveAttribute('aria-label', 'Clear results')
      })
    })
  })
})
