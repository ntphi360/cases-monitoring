import { useEffect, useRef, useState } from "react";
import {
  Bell,
  BellRing,
  ChartNoAxesCombined,
  ChevronDown,
  ChevronRight,
  FileText,
  FolderKanban,
  Import,
  LayoutDashboard,
  Menu,
  ShieldCheck,
  LogOut,
  UserRound,
} from "lucide-react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { signOut } from "../features/auth/authSlice";
import {
  getNotificationPreview,
  markAllNotificationsRead,
  markNotificationRead,
  notifyNotificationsUpdated,
} from "../services/notificationService";

const menuItems = [
  { label: "Dashboard", path: "/", icon: LayoutDashboard },
  { label: "Hồ sơ", path: "/cases", icon: FolderKanban },
  { label: "Cảnh báo", path: "/alerts", icon: BellRing },
  { label: "Thông báo", path: "/notifications", icon: Bell },
  { label: "Thống kê & Báo cáo", path: "/reports", icon: ChartNoAxesCombined },
  { label: "Import dữ liệu", path: "/import", icon: Import, roles: ["ADMIN"] },
  { label: "Người dùng", path: "/users", icon: UserRound, roles: ["ADMIN"] },
];

const pageTitles = {
  "/": "Dashboard",
  "/cases": "Hồ sơ",
  "/alerts": "Cảnh báo",
  "/notifications": "Thông báo",
  "/reports": "Thống kê & Báo cáo",
  "/import": "Import dữ liệu",
  "/users": "Người dùng",
};

function getInitials(fullName) {
  const words = String(fullName || "").trim().split(/\s+/).filter(Boolean);
  if (!words.length) return "?";
  return `${words[0][0]}${words.length > 1 ? words.at(-1)[0] : ""}`.toLocaleUpperCase("vi");
}

function getBreadcrumbs(pathname) {
  if (pathname === "/") return [{ label: "Dashboard", path: "/" }];

  const segments = pathname.split("/").filter(Boolean);

  return segments.map((_, index) => {
    const path = `/${segments.slice(0, index + 1).join("/")}`;
    return {
      label: pageTitles[path] ?? "Chi tiết hồ sơ",
      path,
    };
  });
}

function Sidebar({ isCollapsed, isMobileOpen, onNavigate, roles }) {
  return (
    <aside
      className={`sidebar${isMobileOpen ? " sidebar--mobile-open" : ""}`}
      id="main-sidebar"
    >
      <div className="sidebar__brand">
        <div className="sidebar__logo" aria-hidden="true">
          <FileText size={22} strokeWidth={2.2} />
          <ShieldCheck className="sidebar__logo-badge" size={12} strokeWidth={2.6} />
        </div>
        <div className="sidebar__brand-text">
          <strong>HỆ THỐNG</strong>
          <span>GIÁM SÁT HỒ SƠ</span>
        </div>
      </div>

      <nav className="sidebar__nav" aria-label="Điều hướng chính">
        {menuItems.filter((item) => !item.roles || item.roles.some((role) => roles.includes(role))).map(({ label, path, icon: Icon }) => (
          <NavLink
            className={({ isActive }) =>
              `sidebar__link${isActive ? " sidebar__link--active" : ""}`
            }
            end={path === "/"}
            key={path}
            onClick={onNavigate}
            title={isCollapsed ? label : undefined}
            to={path}
          >
            <Icon className="sidebar__link-icon" size={19} strokeWidth={1.8} />
            <span>{label}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}

function Breadcrumbs({ items }) {
  return (
    <nav className="breadcrumbs" aria-label="Breadcrumb">
      {items.map((item, index) => {
        const isLast = index === items.length - 1;

        return (
          <span className="breadcrumbs__item" key={item.path}>
            {index > 0 && <ChevronRight size={13} aria-hidden="true" />}
            {isLast ? (
              <span aria-current="page">{item.label}</span>
            ) : (
              <Link to={item.path}>{item.label}</Link>
            )}
          </span>
        );
      })}
    </nav>
  );
}

function Header({ breadcrumbs, isMobileOpen, onToggleSidebar }) {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const user = useSelector((state) => state.auth.user);
  const userId = user?.id;
  const primaryRole = user?.roles?.[0] || "";
  const notificationRef = useRef(null);
  const userMenuRef = useRef(null);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState([]);
  const [notificationOpen, setNotificationOpen] = useState(false);
  const [notificationLoading, setNotificationLoading] = useState(true);
  const [notificationError, setNotificationError] = useState("");
  const [notificationActionId, setNotificationActionId] = useState(null);
  const [markingAll, setMarkingAll] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const [avatarFailed, setAvatarFailed] = useState(false);

  useEffect(() => {
    let isCurrent = true;

    function loadNotifications(event) {
      const force = event?.type === "notifications-updated";
      getNotificationPreview(userId, { force })
        .then((response) => {
          if (!isCurrent) return;
          setNotifications(response.results ?? []);
          setUnreadCount(response.unreadCount ?? 0);
          setNotificationError("");
        })
        .catch((error) => {
          if (isCurrent) setNotificationError(error.message || "Không thể tải thông báo.");
        })
        .finally(() => {
          if (isCurrent) setNotificationLoading(false);
        });
    }

    loadNotifications();
    window.addEventListener("notifications-updated", loadNotifications);
    return () => {
      isCurrent = false;
      window.removeEventListener("notifications-updated", loadNotifications);
    };
  }, [userId]);

  useEffect(() => {
    if (!notificationOpen) return undefined;

    function closeOnOutsideClick(event) {
      if (!notificationRef.current?.contains(event.target)) {
        setNotificationOpen(false);
      }
    }

    document.addEventListener("click", closeOnOutsideClick);
    return () => document.removeEventListener("click", closeOnOutsideClick);
  }, [notificationOpen]);

  useEffect(() => {
    if (!userMenuOpen) return undefined;
    function closeOnOutsideClick(event) {
      if (!userMenuRef.current?.contains(event.target)) setUserMenuOpen(false);
    }
    document.addEventListener("click", closeOnOutsideClick);
    return () => document.removeEventListener("click", closeOnOutsideClick);
  }, [userMenuOpen]);

  function formatNotificationTime(value) {
    if (!value) return "—";
    const [date, time = ""] = String(value).split("T");
    const [year, month, day] = date.split("-");
    return `${day}/${month}/${year}${time ? ` ${time.slice(0, 5)}` : ""}`;
  }

  async function openNotification(item) {
    if (notificationActionId !== null) return;
    setNotificationActionId(item.id);
    try {
      if (!item.isRead) {
        await markNotificationRead(item.id);
        setNotifications((current) => current.map((entry) =>
          entry.id === item.id ? { ...entry, isRead: true } : entry));
        setUnreadCount((current) => Math.max(0, current - 1));
        notifyNotificationsUpdated();
      }
      setNotificationOpen(false);
      if (item.caseId) navigate(`/cases/${item.caseId}`);
    } catch (error) {
      setNotificationError(error.message || "Không thể đánh dấu thông báo đã đọc.");
    } finally {
      setNotificationActionId(null);
    }
  }

  async function markAllRead() {
    if (markingAll) return;
    setMarkingAll(true);
    try {
      await markAllNotificationsRead(userId);
      setNotifications((current) => current.map((item) => ({ ...item, isRead: true })));
      setUnreadCount(0);
      notifyNotificationsUpdated();
    } catch (error) {
      setNotificationError(error.message || "Không thể đánh dấu tất cả đã đọc.");
    } finally {
      setMarkingAll(false);
    }
  }

  async function handleLogout() {
    if (loggingOut) return;
    setLoggingOut(true);
    setUserMenuOpen(false);
    await dispatch(signOut());
    navigate("/login", { replace: true });
  }

  return (
    <header className="app-header">
      <div className="app-header__start">
        <button
          aria-controls="main-sidebar"
          aria-label={isMobileOpen ? "Đóng menu" : "Mở hoặc thu gọn menu"}
          className="icon-button app-header__menu"
          type="button"
          onClick={onToggleSidebar}
        >
          <Menu size={20} />
        </button>
        <Breadcrumbs items={breadcrumbs} />
      </div>

      <div className="app-header__actions">
        <div className="notification-menu" ref={notificationRef}>
          <button
            aria-controls="header-notification-dropdown"
            aria-expanded={notificationOpen}
            aria-label={`Thông báo${unreadCount ? `, ${unreadCount} chưa đọc` : ""}`}
            className="icon-button notification-button"
            type="button"
            onClick={() => { setUserMenuOpen(false); setNotificationOpen((isOpen) => !isOpen); }}
          >
            <Bell size={20} />
            {unreadCount > 0 && <span className="notification-button__count">{unreadCount > 99 ? "99+" : unreadCount}</span>}
          </button>

          {notificationOpen && (
            <section className="notification-dropdown" id="header-notification-dropdown">
              <header className="notification-dropdown__header">
                <div><strong>Thông báo</strong><small>{unreadCount} chưa đọc</small></div>
                {unreadCount > 0 && <button disabled={markingAll} type="button" onClick={markAllRead}>{markingAll ? "Đang cập nhật..." : "Đánh dấu tất cả đã đọc"}</button>}
              </header>
              <div className="notification-dropdown__list">
                {notificationLoading && <p className="notification-dropdown__message">Đang tải thông báo...</p>}
                {!notificationLoading && notificationError && <p className="notification-dropdown__message notification-dropdown__message--error" role="alert">{notificationError}</p>}
                {!notificationLoading && !notificationError && notifications.length === 0 && <p className="notification-dropdown__message">Không có thông báo</p>}
                {!notificationLoading && notifications.map((item) => (
                  <button className={`notification-dropdown__item${item.isRead ? " notification-dropdown__item--read" : ""}`} disabled={notificationActionId === item.id} key={item.id} type="button" onClick={() => openNotification(item)}>
                    <span className="notification-dropdown__item-content">
                      <strong>{item.message}</strong>
                      <small>{item.externalCaseCode ? `${item.externalCaseCode} · ` : ""}{formatNotificationTime(item.createdAt)}</small>
                    </span>
                    <span className="notification-dropdown__status">{item.isRead ? "Đã đọc" : "Chưa đọc"}</span>
                  </button>
                ))}
              </div>
              <button className="notification-dropdown__all" type="button" onClick={() => { setNotificationOpen(false); navigate("/notifications"); }}>Xem tất cả</button>
            </section>
          )}
        </div>
        <div className="header-user-menu" ref={userMenuRef}>
          <button aria-expanded={userMenuOpen} aria-haspopup="menu" className="header-user-trigger" type="button" onClick={() => { setNotificationOpen(false); setUserMenuOpen((open) => !open); }}>
            <span className="header-user-avatar">
              {user?.avatarUrl && !avatarFailed ? <img alt="" src={user.avatarUrl} onError={() => setAvatarFailed(true)} /> : getInitials(user?.fullName || user?.userName)}
            </span>
            <span className="header-user-summary"><strong>{user?.fullName || user?.userName}</strong><small>{primaryRole}</small></span>
            <ChevronDown className={userMenuOpen ? "header-user-chevron--open" : ""} size={15} />
          </button>
          {userMenuOpen && <section className="header-user-dropdown" role="menu">
            <div className="header-user-dropdown__profile"><strong>{user?.fullName || user?.userName}</strong><span>{user?.email || "—"}</span><small>{primaryRole}</small></div>
            <button disabled={loggingOut} role="menuitem" type="button" onClick={handleLogout}><LogOut size={16} /> {loggingOut ? "Đang đăng xuất..." : "Đăng xuất"}</button>
          </section>}
        </div>
      </div>
    </header>
  );
}

function MainLayout() {
  const roles = useSelector((state) => state.auth.user?.roles ?? []);
  const { pathname } = useLocation();
  const breadcrumbs = getBreadcrumbs(pathname);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  function toggleSidebar() {
    if (window.matchMedia("(max-width: 760px)").matches) {
      setIsMobileOpen((isOpen) => !isOpen);
      return;
    }

    setIsCollapsed((isOpen) => !isOpen);
  }

  function closeMobileSidebar() {
    setIsMobileOpen(false);
  }

  return (
    <div
      className={`main-layout${isCollapsed ? " main-layout--collapsed" : ""}${
        isMobileOpen ? " main-layout--mobile-open" : ""
      }`}
    >
      <Sidebar
        isCollapsed={isCollapsed}
        isMobileOpen={isMobileOpen}
        onNavigate={closeMobileSidebar}
        roles={roles}
      />
      {isMobileOpen && (
        <button
          aria-label="Đóng menu"
          className="sidebar-overlay"
          type="button"
          onClick={closeMobileSidebar}
        />
      )}
      <div className="main-layout__body">
        <Header
          breadcrumbs={breadcrumbs}
          isMobileOpen={isMobileOpen}
          onToggleSidebar={toggleSidebar}
        />
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default MainLayout;
