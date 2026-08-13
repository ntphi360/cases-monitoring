import { useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { FileText, LogIn, ShieldCheck } from 'lucide-react'
import { signIn } from '../features/auth/authSlice'
import './LoginPage.css'

function LoginPage() {
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const { error, isAuthenticated, loading } = useSelector((state) => state.auth)
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')

  if (isAuthenticated) return <Navigate replace to="/" />

  async function submit(event) {
    event.preventDefault()
    try {
      await dispatch(signIn({ userName: userName.trim(), password })).unwrap()
      navigate(location.state?.from?.pathname || '/', { replace: true })
    } catch { /* Error is rendered from Redux state. */ }
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={submit}>
        <div className="login-card__logo"><FileText size={28} /><ShieldCheck size={15} /></div>
        <div><h1>Đăng nhập</h1><p>Hệ thống giám sát hồ sơ</p></div>
        <label><span>Tên đăng nhập</span><input autoComplete="username" autoFocus required value={userName} onChange={(event) => setUserName(event.target.value)} /></label>
        <label><span>Mật khẩu</span><input autoComplete="current-password" required type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
        {error && <p className="login-card__error" role="alert">{error}</p>}
        <button disabled={loading} type="submit"><LogIn size={17} /> {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
      </form>
    </main>
  )
}

export default LoginPage
