import { env } from './env.js'

export const API_BASE_URL = env.apiBaseUrl.replace(/\/+$/, '')
