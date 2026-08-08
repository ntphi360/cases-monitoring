import { API_BASE_URL } from '../config/api.js'

async function readResponse(response) {
  const text = await response.text()

  if (!text) return null

  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

export async function apiFetch(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, options)
  const data = await readResponse(response)

  if (!response.ok) {
    const message =
      data && typeof data === 'object' && data.message
        ? data.message
        : `Yêu cầu thất bại (${response.status} ${response.statusText})`
    throw new Error(message)
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
