import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from './contexts/AuthContext.jsx'
import './App.css'

function App() {
  const [clientId, setClientId] = useState('')
  const [clientData, setClientData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const navigate = useNavigate()
  const { username, logout } = useAuth()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
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
          <div className="search-header">
            <h2 className="search-title">Search Client Data</h2>
            <p className="search-subtitle">Enter a client ID to retrieve information from the CRM system</p>
          </div>
          <form onSubmit={handleSubmit} className="client-form">
            <div className="form-group">
              <label htmlFor="clientId">Client ID</label>
              <div className="input-wrapper">
                <svg className="input-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="11" cy="11" r="8"></circle>
                  <path d="m21 21-4.35-4.35"></path>
                </svg>
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
            </div>
            <button type="submit" className="search-button" disabled={loading || !clientId.trim()}>
              {loading ? (
                <>
                  <svg className="button-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <line x1="12" y1="2" x2="12" y2="6"></line>
                    <line x1="12" y1="18" x2="12" y2="22"></line>
                    <line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line>
                    <line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line>
                    <line x1="2" y1="12" x2="6" y2="12"></line>
                    <line x1="18" y1="12" x2="22" y2="12"></line>
                    <line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line>
                    <line x1="16.24" y1="7.76" x2="19.07" y2="4.93"></line>
                  </svg>
                  Searching...
                </>
              ) : (
                <>
                  <svg className="button-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="11" cy="11" r="8"></circle>
                    <path d="m21 21-4.35-4.35"></path>
                  </svg>
                  Search
                </>
              )}
            </button>
          </form>
        </div>

        {error && (
          <div className="error-card">
            <div className="error-icon-wrapper">
              <svg className="error-icon" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="8" x2="12" y2="12"></line>
                <line x1="12" y1="16" x2="12.01" y2="16"></line>
              </svg>
            </div>
            <div className="error-content">
              <h3>Error</h3>
              <p>{error}</p>
            </div>
          </div>
        )}

        {clientData && (
          <div className="results-card">
            <div className="results-header">
              <div className="results-title-wrapper">
                <svg className="results-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"></path>
                  <circle cx="9" cy="7" r="4"></circle>
                  <path d="M22 21v-2a4 4 0 0 0-3-3.87"></path>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                </svg>
                <div>
                  <h2>Client Data Results</h2>
                  <p className="results-subtitle">Retrieved successfully</p>
                </div>
              </div>
              <button 
                onClick={() => setClientData(null)} 
                className="clear-button"
                aria-label="Clear results"
                title="Clear results"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18"></line>
                  <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
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
