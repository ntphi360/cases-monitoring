import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useSelector } from 'react-redux'

export function ProtectedRoute() {
  const { initialized, isAuthenticated, loading } = useSelector((state) => state.auth)
  const location = useLocation()
  if (loading || !initialized) return <div className="auth-loading">Đang khôi phục phiên đăng nhập...</div>
  return isAuthenticated
    ? <Outlet />
    : <Navigate replace state={{ from: location }} to="/login" />
}

export function AdminRoute() {
  const roles = useSelector((state) => state.auth.user?.roles ?? [])
  return roles.includes('ADMIN') ? <Outlet /> : <Navigate replace to="/" />
}
