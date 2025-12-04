import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Input, Card, CardHeader, CardBody, Alert, Badge } from '@frankcrum/earth-react'
import './App.css'

function App() {
  const [clientId, setClientId] = useState('')
  const [clientData, setClientData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const navigate = useNavigate()
  const username = localStorage.getItem('username') || 'User'

  const handleLogout = () => {
    localStorage.removeItem('isAuthenticated')
    localStorage.removeItem('username')
    navigate('/login')
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setClientData(null)

    try {
      const response = await fetch(`/api/Crm/GetClientData?id=${encodeURIComponent(clientId)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Failed to fetch client data')
      }

      const result = await response.json()
      
      // Try to parse JSON if it's a string
      let parsedData = result.data
      if (typeof parsedData === 'string') {
        try {
          parsedData = JSON.parse(parsedData)
        } catch {
          // Keep as string if not valid JSON
        }
      }
      
      setClientData(parsedData)
    } catch (err) {
      setError(err.message || 'An error occurred while fetching client data')
    } finally {
      setLoading(false)
    }
  }

  const formatClientData = (data) => {
    if (typeof data === 'string') {
      return data
    }
    
    if (typeof data === 'object' && data !== null) {
      return Object.entries(data).map(([key, value]) => (
        <div key={key} className="data-row">
          <span className="data-key">{key}:</span>
          <span className="data-value">
            {typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value)}
          </span>
        </div>
      ))
    }
    
    return JSON.stringify(data, null, 2)
  }

  return (
    <div className="app-container">
      <header className="app-header">
        <div className="header-content">
          <h1>CRM Client Data Fetcher</h1>
          <div className="header-actions">
            <span className="username">Welcome, {username}</span>
            <button onClick={handleLogout} className="logout-button">
              Logout
            </button>
          </div>
        </div>
      </header>

      <main className="app-main">
        <div className="search-section">
          <form onSubmit={handleSubmit} className="client-form">
            <div className="form-group">
              <label htmlFor="clientId">Client ID</label>
              <input
                type="text"
                id="clientId"
                value={clientId}
                onChange={(e) => setClientId(e.target.value)}
                placeholder="Enter client ID to search"
                required
                disabled={loading}
              />
            </div>
            <button type="submit" className="search-button" disabled={loading || !clientId.trim()}>
              {loading ? 'Searching...' : 'Search'}
            </button>
          </form>
        </div>

        {error && (
          <div className="error-card">
            <div className="error-icon">⚠️</div>
            <div className="error-content">
              <h3>Error</h3>
              <p>{error}</p>
            </div>
          </div>
        )}

        {clientData && (
          <div className="results-card">
            <div className="results-header">
              <h2>Client Data Results</h2>
              <button 
                onClick={() => setClientData(null)} 
                className="clear-button"
                aria-label="Clear results"
              >
                ✕
              </button>
            </div>
            <div className="results-content">
              {typeof clientData === 'object' && clientData !== null ? (
                <div className="data-grid">
                  {formatClientData(clientData)}
                </div>
              ) : (
                <div className="data-text">
                  {formatClientData(clientData)}
                </div>
              )}
            </div>
          </div>
        )}
      </main>
    </div>
  )
}

export default App
