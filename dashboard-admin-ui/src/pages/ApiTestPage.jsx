import { useEffect, useState } from 'react'
import { API_BASE_URL } from '../config/api.js'
import './ApiTestPage.css'

const endpoints = [
  {
    key: 'cases',
    title: 'Cases',
    path: '/cases/paging?pageIndex=1&pageSize=10',
  },
  { key: 'departments', title: 'Departments', path: '/departments' },
  {
    key: 'procedure-fields',
    title: 'Procedure Fields',
    path: '/procedurefields',
  },
  { key: 'procedures', title: 'Procedures', path: '/procedures' },
  { key: 'users', title: 'Users', path: '/users' },
  {
    key: 'case-assignments',
    title: 'Case Assignments - Case #2',
    path: '/caseassignments/by-case/2',
  },
  {
    key: 'case-histories',
    title: 'Case Histories - Case #2',
    path: '/casehistories/by-case/2',
  },
]

const initialResults = Object.fromEntries(
  endpoints.map(({ key }) => [key, { state: 'loading' }]),
)

async function requestEndpoint(path, signal) {
  const response = await fetch(`${API_BASE_URL}${path}`, { signal })
  const responseText = await response.text()
  let data = null

  if (responseText) {
    try {
      data = JSON.parse(responseText)
    } catch {
      data = responseText
    }
  }

  if (!response.ok) {
    const error = new Error(`HTTP ${response.status} ${response.statusText}`)
    error.status = response.status
    error.statusText = response.statusText
    error.data = data
    throw error
  }

  return {
    data,
    status: response.status,
    statusText: response.statusText,
  }
}

function ApiTestPage() {
  const [results, setResults] = useState(initialResults)

  useEffect(() => {
    const controller = new AbortController()

    endpoints.forEach(({ key, path }) => {
      requestEndpoint(path, controller.signal)
        .then((result) => {
          setResults((current) => ({
            ...current,
            [key]: { state: 'success', ...result },
          }))
        })
        .catch((error) => {
          if (error.name === 'AbortError') return

          setResults((current) => ({
            ...current,
            [key]: {
              state: 'error',
              message: error.message || 'Failed to fetch',
              status: error.status,
              statusText: error.statusText,
              data: error.data,
            },
          }))
        })
    })

    return () => controller.abort()
  }, [])

  return (
    <main className="api-test-page">
      <h1>API Test</h1>
      <p className="api-test-base-url">
        Base URL: <code>{API_BASE_URL}</code>
      </p>

      {endpoints.map(({ key, title, path }) => {
        const result = results[key]

        return (
          <section className="api-test-result" key={key}>
            <h2>{title}</h2>
            <p className="api-test-endpoint">GET {path}</p>
            {result.state === 'loading' && <p>Status: Loading...</p>}
            {result.state === 'success' && (
              <>
                <p className="api-test-success">
                  Status: {result.status} {result.statusText}
                </p>
                <pre>{JSON.stringify(result.data, null, 2)}</pre>
              </>
            )}
            {result.state === 'error' && (
              <>
                <p className="api-test-error">
                  Status:{' '}
                  {result.status
                    ? `${result.status} ${result.statusText}`
                    : result.message}
                </p>
                {result.data !== undefined && (
                  <pre>{JSON.stringify(result.data, null, 2)}</pre>
                )}
              </>
            )}
          </section>
        )
      })}
    </main>
  )
}

export default ApiTestPage
