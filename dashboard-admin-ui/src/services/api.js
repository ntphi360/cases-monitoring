import { API_BASE_URL } from '../config/api.js'

let accessToken = null
let refreshPromise = null

export function setApiAccessToken(token) {
  accessToken = token || null
}

async function readResponse(response) {
  const text = await response.text()

  if (!text) return null

  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

async function request(path, options = {}) {
  const fetchOptions = { ...options }
  delete fetchOptions.skipAuthRefresh
  const headers = new Headers(options.headers || {})
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  let response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...fetchOptions,
      headers,
      credentials: 'include',
    })
  } catch {
    throw new Error('Không thể kết nối máy chủ. Vui lòng thử lại.')
  }
  return { response, data: await readResponse(response) }
}

export async function refreshApiSession({ notify = true } = {}) {
  if (!refreshPromise) {
    refreshPromise = request('/auth/refresh', { method: 'POST', skipAuthRefresh: true })
      .then(({ response, data }) => {
        if (!response.ok) throw new Error('Phiên đăng nhập đã hết hạn.')
        setApiAccessToken(data?.data?.accessToken)
        if (notify) {
          window.dispatchEvent(new CustomEvent('auth-refreshed', { detail: data.data }))
        }
        return data.data
      })
      .finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

export async function apiFetch(path, options = {}) {
  let { response, data } = await request(path, options)

  if (response.status === 401 && !options.skipAuthRefresh && !path.startsWith('/auth/')) {
    try {
      await refreshApiSession()
      ;({ response, data } = await request(path, { ...options, skipAuthRefresh: true }))
    } catch {
      setApiAccessToken(null)
      window.dispatchEvent(new Event('auth-expired'))
    }
  }

  if (!response.ok) {
    const message =
      data && typeof data === 'object' && data.message
        ? data.message
        : response.status === 403
          ? 'Bạn không có quyền thực hiện chức năng này.'
        : `Yêu cầu thất bại (${response.status} ${response.statusText})`
    const error = new Error(message)
    error.status = response.status
    throw error
  }

  return data
}

export function postFormData(path, formData, options = {}) {
  return apiFetch(path, {
    ...options,
    method: 'POST',
    body: formData,
  })
}
