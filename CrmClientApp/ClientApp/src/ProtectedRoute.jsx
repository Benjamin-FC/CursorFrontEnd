import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './contexts/AuthContext.jsx'

function ProtectedRoute({ children }) {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()
  
  // Show loading state while checking authentication
  if (isLoading) {
    return <div>Loading...</div>
  }
  
  if (!isAuthenticated) {
    // Always redirect to login, preserving the attempted location
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  
  return children
}

export default ProtectedRoute
