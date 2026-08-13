import { apiFetch } from './api'

export function getManagedUsers() {
  return apiFetch('/Users/management')
}

export function createUser(payload) {
  return apiFetch('/Users', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
}

export function updateUser(id, payload) {
  return apiFetch(`/Users/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
}

export function setUserActive(id, isActive) {
  return apiFetch(`/Users/${id}/active`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isActive }),
  })
}
