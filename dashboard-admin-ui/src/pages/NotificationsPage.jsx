import { Bell, CheckCheck, ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";

import {
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from "../services/notificationService";
import "./NotificationsPage.css";

const PAGE_SIZE = 20;

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time = ""] = String(value).split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year}${time ? ` ${time.slice(0, 5)}` : ""}`;
}

function notifyUnreadChanged() {
  window.dispatchEvent(new Event("notifications-updated"));
}

function NotificationsPage() {
  const navigate = useNavigate();
  const userId = useSelector((state) => state.auth.user?.id);
  const [items, setItems] = useState([]);
  const [isRead, setIsRead] = useState("");
  const [pageIndex, setPageIndex] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let isCurrent = true;

    getNotifications({ userId, isRead, pageIndex, pageSize: PAGE_SIZE })
      .then((response) => {
        if (!isCurrent) return;
        setItems(response.results ?? []);
        setTotalPages(Math.max(response.totalPages ?? 0, 1));
        setUnreadCount(response.unreadCount ?? 0);
      })
      .catch((loadError) => {
        if (isCurrent) setError(loadError.message || "Không thể tải thông báo.");
      })
      .finally(() => {
        if (isCurrent) setLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, [isRead, pageIndex, userId]);

  async function openNotification(item) {
    try {
      if (!item.isRead) {
        await markNotificationRead(item.id);
        setItems((current) => current.map((entry) =>
          entry.id === item.id ? { ...entry, isRead: true } : entry));
        setUnreadCount((current) => Math.max(0, current - 1));
        notifyUnreadChanged();
      }
      if (item.caseId) navigate(`/cases/${item.caseId}`);
    } catch (updateError) {
      setError(updateError.message || "Không thể đánh dấu thông báo đã đọc.");
    }
  }

  async function markAllRead() {
    try {
      await markAllNotificationsRead(userId);
      setItems((current) => current.map((item) => ({ ...item, isRead: true })));
      setUnreadCount(0);
      notifyUnreadChanged();
    } catch (updateError) {
      setError(updateError.message || "Không thể đánh dấu tất cả đã đọc.");
    }
  }

  function changeReadFilter(value) {
    setLoading(true);
    setError("");
    setIsRead(value);
    setPageIndex(1);
  }

  function changePage(nextPage) {
    setLoading(true);
    setError("");
    setPageIndex(nextPage);
  }

  return (
    <section className="notifications-page">
      <div className="notifications-page__heading">
        <div><h1>Thông báo</h1><p>{unreadCount} thông báo chưa đọc</p></div>
        <button className="notifications-button" disabled={unreadCount === 0} type="button" onClick={markAllRead}>
          <CheckCheck size={16} /> Đánh dấu tất cả đã đọc
        </button>
      </div>

      <div className="notifications-filter" role="group" aria-label="Lọc thông báo">
        {[['', 'Tất cả'], ['false', 'Chưa đọc'], ['true', 'Đã đọc']].map(([value, label]) => (
          <button className={isRead === value ? "is-active" : ""} key={label} type="button" onClick={() => changeReadFilter(value)}>{label}</button>
        ))}
      </div>

      <article className="notifications-card">
        {loading && <p className="notifications-message">Đang tải thông báo...</p>}
        {!loading && error && <p className="notifications-message notifications-message--error" role="alert">Lỗi: {error}</p>}
        {!loading && !error && items.length === 0 && <p className="notifications-message">Không có thông báo</p>}
        {!loading && !error && items.map((item) => (
          <button className={`notification-item${item.isRead ? " notification-item--read" : ""}`} key={item.id} type="button" onClick={() => openNotification(item)}>
            <span className="notification-item__icon"><Bell size={17} /></span>
            <span className="notification-item__content">
              <strong>{item.message}</strong>
              <small>{item.externalCaseCode ? `Hồ sơ: ${item.externalCaseCode} · ` : ""}{formatDateTime(item.createdAt)}</small>
            </span>
            <span className="notification-item__status">{item.isRead ? "Đã đọc" : "Chưa đọc"}</span>
          </button>
        ))}
        <footer className="notifications-pagination">
          <span>Trang {pageIndex} / {totalPages}</span>
          <div>
            <button aria-label="Trang trước" disabled={loading || pageIndex === 1} type="button" onClick={() => changePage(pageIndex - 1)}><ChevronLeft size={16} /></button>
            <button aria-label="Trang sau" disabled={loading || pageIndex === totalPages} type="button" onClick={() => changePage(pageIndex + 1)}><ChevronRight size={16} /></button>
          </div>
        </footer>
      </article>
    </section>
  );
}

export default NotificationsPage;
