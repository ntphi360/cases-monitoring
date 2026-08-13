import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { loginUser, logoutUser, restoreSession } from '../../services/authService'

export const signIn = createAsyncThunk('auth/signIn', loginUser)
export const restoreAuth = createAsyncThunk('auth/restore', restoreSession)
export const signOut = createAsyncThunk('auth/signOut', logoutUser)

const initialState = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  loading: true,
  initialized: false,
  error: '',
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    sessionRefreshed(state, action) {
      state.user = action.payload.user
      state.accessToken = action.payload.accessToken
      state.isAuthenticated = true
      state.initialized = true
      state.loading = false
    },
    sessionExpired(state) {
      Object.assign(state, { ...initialState, loading: false, initialized: true })
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(signIn.pending, (state) => { state.loading = true; state.error = '' })
      .addCase(signIn.fulfilled, (state, action) => {
        state.user = action.payload.user
        state.accessToken = action.payload.accessToken
        state.isAuthenticated = true
        state.loading = false
        state.initialized = true
      })
      .addCase(signIn.rejected, (state, action) => {
        state.loading = false
        state.initialized = true
        state.error = action.error.message || 'Không thể đăng nhập.'
      })
      .addCase(restoreAuth.fulfilled, (state, action) => {
        state.user = action.payload.user
        state.accessToken = action.payload.accessToken
        state.isAuthenticated = true
        state.loading = false
        state.initialized = true
      })
      .addCase(restoreAuth.rejected, (state) => {
        state.loading = false
        state.initialized = true
      })
      .addCase(signOut.fulfilled, () => ({ ...initialState, loading: false, initialized: true }))
      .addCase(signOut.rejected, () => ({ ...initialState, loading: false, initialized: true }))
  },
})

export const { sessionExpired, sessionRefreshed } = authSlice.actions
export default authSlice.reducer
