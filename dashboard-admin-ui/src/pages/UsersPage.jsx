import { useEffect, useMemo, useState } from 'react'
import { LockKeyhole, Pencil, Search, UnlockKeyhole, UserPlus, Users, X } from 'lucide-react'
import { getDepartments } from '../services/caseService'
import { createUser, getManagedUsers, setUserActive, updateUser } from '../services/userService'
import './CasesPage.css'
import './UsersPage.css'

const emptyForm = {
  fullName: '', userName: '', email: '', phoneNumber: '', role: 'STAFF',
  password: '', departmentId: '', isActive: true,
}

function UserModal({ departments, editingUser, onClose, onSaved }) {
  const [form, setForm] = useState(() => editingUser ? {
    fullName: editingUser.fullName,
    userName: editingUser.username,
    email: editingUser.email,
    phoneNumber: editingUser.phoneNumber || '',
    role: editingUser.roles?.[0] || 'STAFF',
    departmentId: String(editingUser.departmentId),
    isActive: editingUser.isActive,
    password: '',
  } : emptyForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function update(name, value) {
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      const common = {
        fullName: form.fullName.trim(), email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim() || null, role: form.role,
        departmentId: Number(form.departmentId), isActive: form.isActive,
      }
      const saved = editingUser
        ? await updateUser(editingUser.id, common)
        : await createUser({ ...common, userName: form.userName.trim(), password: form.password })
      onSaved(saved)
    } catch (saveError) {
      setError(saveError.message || 'Không thể lưu tài khoản.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="case-modal" role="presentation" onMouseDown={saving ? undefined : onClose}>
      <section aria-modal="true" className="case-modal__dialog user-modal" role="dialog" onMouseDown={(event) => event.stopPropagation()}>
        <header className="case-modal__header"><h2>{editingUser ? 'Cập nhật người dùng' : 'Tạo người dùng'}</h2><button aria-label="Đóng" disabled={saving} type="button" onClick={onClose}><X size={17} /></button></header>
        <form onSubmit={submit}>
          <div className="case-modal__body case-form-grid">
            <label><span>Họ tên <em>*</em></span><input disabled={saving} required value={form.fullName} onChange={(event) => update('fullName', event.target.value)} /></label>
            <label><span>Username <em>*</em></span><input autoComplete="off" disabled={saving || Boolean(editingUser)} required value={form.userName} onChange={(event) => update('userName', event.target.value)} /></label>
            <label><span>Email <em>*</em></span><input disabled={saving} required type="email" value={form.email} onChange={(event) => update('email', event.target.value)} /></label>
            <label><span>Số điện thoại</span><input disabled={saving} value={form.phoneNumber} onChange={(event) => update('phoneNumber', event.target.value)} /></label>
            <label><span>Vai trò <em>*</em></span><select disabled={saving} value={form.role} onChange={(event) => update('role', event.target.value)}><option value="STAFF">STAFF</option><option value="ADMIN">ADMIN</option></select></label>
            <label><span>Phòng ban <em>*</em></span><select disabled={saving} required value={form.departmentId} onChange={(event) => update('departmentId', event.target.value)}><option value="">Chọn phòng ban</option>{departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            {!editingUser && <label className="case-form-grid__wide"><span>Mật khẩu ban đầu <em>*</em></span><input autoComplete="new-password" disabled={saving} minLength="8" required type="password" value={form.password} onChange={(event) => update('password', event.target.value)} /></label>}
            <label className="user-form__active"><input checked={form.isActive} disabled={saving} type="checkbox" onChange={(event) => update('isActive', event.target.checked)} /><span>Tài khoản đang hoạt động</span></label>
            {error && <p className="case-form-error case-form-grid__wide" role="alert">{error}</p>}
          </div>
          <footer className="case-modal__footer"><button className="cases-button cases-button--secondary" disabled={saving} type="button" onClick={onClose}>Hủy</button><button className="cases-button cases-button--primary" disabled={saving} type="submit">{saving ? 'Đang lưu...' : 'Lưu'}</button></footer>
        </form>
      </section>
    </div>
  )
}

function UsersPage() {
  const [users, setUsers] = useState([])
  const [departments, setDepartments] = useState([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [actionError, setActionError] = useState('')
  const [search, setSearch] = useState('')
  const [role, setRole] = useState('')
  const [active, setActive] = useState('')
  const [editingUser, setEditingUser] = useState(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [updatingId, setUpdatingId] = useState(null)

  useEffect(() => {
    let current = true
    Promise.all([getManagedUsers(), getDepartments()])
      .then(([userList, departmentList]) => {
        if (!current) return
        setUsers(userList ?? [])
        setDepartments((departmentList ?? []).filter((item) => item.isActive !== false))
      })
      .catch((error) => { if (current) setLoadError(error.message || 'Không thể tải danh sách người dùng.') })
      .finally(() => { if (current) setLoading(false) })
    return () => { current = false }
  }, [])

  const filteredUsers = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi')
    return users.filter((user) => (!keyword || [user.fullName, user.username, user.email].some((value) => value?.toLocaleLowerCase('vi').includes(keyword)))
      && (!role || user.roles?.includes(role))
      && (active === '' || user.isActive === (active === 'true')))
  }, [active, role, search, users])

  function openCreate() { setEditingUser(null); setModalOpen(true); setActionError('') }
  function openEdit(user) { setEditingUser(user); setModalOpen(true); setActionError('') }
  function saved(user) {
    setUsers((current) => current.some((item) => item.id === user.id)
      ? current.map((item) => item.id === user.id ? user : item)
      : [...current, user].sort((a, b) => a.fullName.localeCompare(b.fullName, 'vi')))
    setModalOpen(false)
  }

  async function toggleActive(user) {
    setUpdatingId(user.id)
    setActionError('')
    try {
      const updated = await setUserActive(user.id, !user.isActive)
      setUsers((current) => current.map((item) => item.id === user.id ? updated : item))
    } catch (error) {
      setActionError(error.message || 'Không thể cập nhật trạng thái tài khoản.')
    } finally {
      setUpdatingId(null)
    }
  }

  return (
    <section className="cases-page users-page">
      <div className="cases-page__heading"><div><h1>Quản lý người dùng</h1><p>Quản lý tài khoản, vai trò và trạng thái truy cập hệ thống.</p></div><button className="cases-button cases-button--primary" type="button" onClick={openCreate}><UserPlus size={15} /> Tạo người dùng</button></div>
      <div className="cases-filter-card users-filter-card">
        <label className="cases-filter-card__search"><span>Tìm kiếm</span><div className="cases-search-input"><Search size={15} /><input placeholder="Họ tên, username hoặc email" value={search} onChange={(event) => setSearch(event.target.value)} /></div></label>
        <label><span>Vai trò</span><select value={role} onChange={(event) => setRole(event.target.value)}><option value="">Tất cả vai trò</option><option value="ADMIN">ADMIN</option><option value="STAFF">STAFF</option></select></label>
        <label><span>Trạng thái</span><select value={active} onChange={(event) => setActive(event.target.value)}><option value="">Tất cả trạng thái</option><option value="true">Hoạt động</option><option value="false">Đã khóa</option></select></label>
      </div>
      {actionError && <p className="users-page__error" role="alert">{actionError}</p>}
      <article className="cases-table-card"><div className="cases-table-card__header"><h2>Danh sách người dùng</h2><span>{loading ? 'Đang tải...' : `${filteredUsers.length} người dùng`}</span></div><div className="cases-table-wrap"><table className="cases-table users-table"><thead><tr><th>Họ tên</th><th>Username</th><th>Email</th><th>Số điện thoại</th><th>Role</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>
        {loading && <tr><td className="cases-table__empty" colSpan="7">Đang tải danh sách người dùng...</td></tr>}
        {!loading && loadError && <tr><td className="cases-table__empty" colSpan="7" role="alert">Lỗi: {loadError}</td></tr>}
        {!loading && !loadError && filteredUsers.map((user) => <tr key={user.id}><td className="users-table__name">{user.fullName}</td><td>{user.username}</td><td>{user.email}</td><td>{user.phoneNumber || '—'}</td><td><span className={`user-role user-role--${user.roles?.[0]?.toLowerCase()}`}>{user.roles?.[0] || '—'}</span></td><td><span className={`user-status user-status--${user.isActive ? 'active' : 'locked'}`}>{user.isActive ? 'Hoạt động' : 'Đã khóa'}</span></td><td><div className="cases-row-actions"><button aria-label={`Sửa ${user.fullName}`} className="cases-action-button" title="Chỉnh sửa" type="button" onClick={() => openEdit(user)}><Pencil size={14} /></button><button aria-label={`${user.isActive ? 'Khóa' : 'Mở khóa'} ${user.fullName}`} className={`cases-action-button ${user.isActive ? 'users-action--lock' : 'users-action--unlock'}`} disabled={updatingId === user.id} title={user.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'} type="button" onClick={() => toggleActive(user)}>{user.isActive ? <LockKeyhole size={14} /> : <UnlockKeyhole size={14} />}</button></div></td></tr>)}
        {!loading && !loadError && !filteredUsers.length && <tr><td className="cases-table__empty" colSpan="7"><Users size={22} /><span>Không có người dùng phù hợp</span></td></tr>}
      </tbody></table></div></article>
      {modalOpen && <UserModal departments={departments} editingUser={editingUser} onClose={() => setModalOpen(false)} onSaved={saved} />}
    </section>
  )
}

export default UsersPage
