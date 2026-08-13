import { useEffect } from 'react'
import { useDispatch } from 'react-redux'
import AppRoutes from "./routes/AppRoutes";
import { restoreAuth, sessionExpired, sessionRefreshed } from './features/auth/authSlice'

let authRestoreStarted = false

function App() {
  const dispatch = useDispatch()

  useEffect(() => {
    if (!authRestoreStarted) {
      authRestoreStarted = true
      dispatch(restoreAuth())
    }
    const refreshed = (event) => dispatch(sessionRefreshed(event.detail))
    const expired = () => dispatch(sessionExpired())
    window.addEventListener('auth-refreshed', refreshed)
    window.addEventListener('auth-expired', expired)
    return () => {
      window.removeEventListener('auth-refreshed', refreshed)
      window.removeEventListener('auth-expired', expired)
    }
  }, [dispatch])

  return <AppRoutes />;
}

export default App;
