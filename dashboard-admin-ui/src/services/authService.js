import { apiFetch, refreshApiSession, setApiAccessToken } from './api'

export async function loginUser(credentials) {
  const response = await apiFetch('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
    skipAuthRefresh: true,
  })
  setApiAccessToken(response.data.accessToken)
  return response.data
}

export function restoreSession() {
  return refreshApiSession({ notify: false }).then(async (session) => {
    try {
      const response = await apiFetch('/auth/me', { skipAuthRefresh: true })
      return { ...session, user: response.data ?? response }
    } catch (error) {
      setApiAccessToken(null)
      throw error
    }
  })
}

export async function logoutUser() {
  try {
    await apiFetch('/auth/logout', { method: 'POST', skipAuthRefresh: true })
  } finally {
    setApiAccessToken(null)
  }
}
