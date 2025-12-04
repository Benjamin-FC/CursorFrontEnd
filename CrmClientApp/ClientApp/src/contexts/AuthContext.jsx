import { createContext, useContext, useState, useEffect } from 'react'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [username, setUsername] = useState(null)
  const [isLoading, setIsLoading] = useState(true)

  // Check authentication state on mount
  useEffect(() => {
    const auth = localStorage.getItem('isAuthenticated') === 'true'
    const user = localStorage.getItem('username')
    
    setIsAuthenticated(auth)
    setUsername(user)
    setIsLoading(false)
  }, [])

  const login = async (user, password) => {
    // TODO: Replace with actual authentication API call
    // For now, simple validation with minimal delay to allow loading state to render
    await new Promise(resolve => setTimeout(resolve, 10))
    
    if (user && password) {
      localStorage.setItem('isAuthenticated', 'true')
      localStorage.setItem('username', user)
      setIsAuthenticated(true)
      setUsername(user)
      return { success: true }
    } else {
      return { success: false, error: 'Please enter both username and password' }
    }
  }

  const logout = () => {
    localStorage.removeItem('isAuthenticated')
    localStorage.removeItem('username')
    setIsAuthenticated(false)
    setUsername(null)
  }

  const value = {
    isAuthenticated,
    username,
    isLoading,
    login,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
