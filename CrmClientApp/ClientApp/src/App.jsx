import { useState } from 'react'
import './App.css'

function App() {
  const [clientId, setClientId] = useState('')
  const [clientData, setClientData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

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
      setClientData(result.data)
    } catch (err) {
      setError(err.message || 'An error occurred while fetching client data')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="app-container">
      <h1>CRM Client Data Fetcher</h1>
      <form onSubmit={handleSubmit} className="client-form">
        <div className="form-group">
          <label htmlFor="clientId">Client ID:</label>
          <input
            type="text"
            id="clientId"
            value={clientId}
            onChange={(e) => setClientId(e.target.value)}
            placeholder="Enter client ID"
            required
            disabled={loading}
          />
        </div>
        <button type="submit" disabled={loading || !clientId.trim()}>
          {loading ? 'Loading...' : 'Get Client Data'}
        </button>
      </form>

      {error && (
        <div className="error-message">
          <strong>Error:</strong> {error}
        </div>
      )}

      {clientData && (
        <div className="client-data">
          <h2>Client Data:</h2>
          <pre>{typeof clientData === 'string' ? clientData : JSON.stringify(clientData, null, 2)}</pre>
        </div>
      )}
    </div>
  )
}

export default App
