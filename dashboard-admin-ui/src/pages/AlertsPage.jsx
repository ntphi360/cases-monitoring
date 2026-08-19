import { useEffect, useRef, useState } from "react";
import { useSelector } from "react-redux";
import { BellRing, CalendarClock, ChevronLeft, ChevronRight, Clock3, Eye, Filter, RotateCcw, Search, Send, TriangleAlert, X } from "lucide-react";

import { getCaseAlerts } from "../services/alertService";
import { getLastImportSync } from "../services/importService";
import {
  getDepartments,
  getProcedureFields,
  getProcedures,
  getUsers,
} from "../services/caseService";
import { sendReminder } from "../services/reminderService";
import { notifyNotificationsUpdated } from "../services/notificationService";
import { getDeadlineStatusLabel } from "../utils/caseLabels";
import "./CasesPage.css";
import "./AlertsPage.css";

const PAGE_SIZE = 10;
const alertLevels = [
  { label: "Sắp hạn", value: "Upcoming" },
  { label: "Đến hạn hôm nay", value: "DueToday" },
  { label: "Quá hạn", value: "Overdue" },
];
const reminderChannels = [
  { id: "System", label: "Thông báo hệ thống" },
  { id: "Email", label: "Email" },
  { id: "Zalo", label: "Zalo" },
];
const emptyFilters = {
  search: "",
  fieldId: "",
  procedureId: "",
  departmentId: "",
  assignedUserId: "",
  alertLevel: "",
};

function mapApiAlert(item) {
  return {
    id: item.id,
    caseCode: item.externalCaseCode,
    caseName: item.applicantName,
    procedureFieldName: item.procedureFieldName,
    procedureName: item.procedureName,
    departmentName: item.departmentName,
    assigneeName: item.assigneeName,
    currentAssigneeId: item.currentAssigneeId,
    receivedAt: item.receivedAt,
    dueDate: item.deadline,
    appointmentReturnDate: item.appointmentDate,
    completedAt: item.completedAt,
    status: item.status,
    deadlineStatus: item.deadlineStatus,
  };
}

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time = "00:00"] = value.replace(" ", "T").split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year} ${time.slice(0, 5)}`;
}

function formatLastUpdated(value) {
  if (!value) return "Chưa có lần import thành công";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" })
    .format(new Date(value));
}

function getAlertLevel(deadline, now) {
  if (!deadline) return "Không xác định";
  const due = new Date(deadline);
  const current = new Date(now);

  if (due.getTime() < now) return "Quá hạn";
  if (
    due.getFullYear() === current.getFullYear()
    && due.getMonth() === current.getMonth()
    && due.getDate() === current.getDate()
  ) return "Đến hạn hôm nay";
  return "Sắp hạn";
}

function getAlertKey(level) {
  return {
    "Sắp hạn": "upcoming",
    "Đến hạn hôm nay": "today",
    "Quá hạn": "overdue",
  }[level] ?? "upcoming";
}

function formatCountdown(caseItem, now) {
  if (caseItem.status === 4) return getDeadlineStatusLabel(caseItem.deadlineStatus);

  const deadline = caseItem.dueDate;
  if (!deadline) return "Không xác định";
  const difference = new Date(deadline).getTime() - now;
  if (Number.isNaN(difference)) return "Không xác định";
  if (Math.abs(difference) < 1000) return "Đến hạn";

  const totalSeconds = Math.floor(Math.abs(difference) / 1000);
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const pad = (value) => String(value).padStart(2, "0");
  const formattedDays = difference < 0 ? pad(days) : days;
  const duration = days > 0
    ? `${formattedDays} ngày ${pad(hours)} giờ ${pad(minutes)} phút ${pad(seconds)} giây`
    : `${pad(hours)} giờ ${pad(minutes)} phút ${pad(seconds)} giây`;

  return difference < 0 ? `Quá hạn ${duration}` : `Còn ${duration}`;
}

function AlertBadge({ level }) {
  return <span className={`alert-badge alert-badge--${getAlertKey(level)}`}>{level}</span>;
}

function AlertModal({ children, onClose, title }) {
  return (
    <div className="case-modal" role="presentation" onMouseDown={onClose}>
      <section aria-labelledby="alert-modal-title" aria-modal="true" className="case-modal__dialog alert-modal__dialog" role="dialog" onMouseDown={(event) => event.stopPropagation()}>
        <header className="case-modal__header">
          <h2 id="alert-modal-title">{title}</h2>
          <button aria-label="Đóng" className="case-icon-button" type="button" onClick={onClose}><X size={18} /></button>
        </header>
        {children}
      </section>
    </div>
  );
}

function CaseAlertDetails({ caseItem, now, onClose, onRemind }) {
  const isAdmin = useSelector((state) => state.auth.user?.roles?.includes("ADMIN") ?? false);
  const alertLevel = getAlertLevel(caseItem.dueDate, now);
  const details = [
    ["Mã hồ sơ", caseItem.caseCode],
    ["Tên hồ sơ", caseItem.caseName],
    ["Lĩnh vực", caseItem.procedureFieldName],
    ["Thủ tục hành chính", caseItem.procedureName],
    ["Phòng ban", caseItem.departmentName],
    ["Người xử lý", caseItem.assigneeName],
    ["Hạn xử lý", formatDateTime(caseItem.dueDate)],
    ["Ngày hẹn trả", formatDateTime(caseItem.appointmentReturnDate)],
    ["Thời hạn", formatCountdown(caseItem, now)],
  ];

  return (
    <AlertModal onClose={onClose} title="Thông tin hồ sơ cảnh báo">
      <div className="case-modal__body">
        <dl className="case-details">
          {details.map(([label, value]) => <div className="case-details__item" key={label}><dt>{label}</dt><dd>{value || "—"}</dd></div>)}
          <div className="case-details__item"><dt>Mức cảnh báo</dt><dd><AlertBadge level={alertLevel} /></dd></div>
        </dl>
      </div>
      <footer className="case-modal__footer">
        {isAdmin && <button className="cases-button cases-button--primary" disabled={!caseItem.currentAssigneeId} type="button" onClick={() => onRemind(caseItem)}><Send size={14} /> Gửi nhắc nhở</button>}
        <button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Đóng</button>
      </footer>
    </AlertModal>
  );
}

function createDefaultReminder(caseItem, now) {
  const appointmentDate = caseItem.appointmentReturnDate
    ? formatDateTime(caseItem.appointmentReturnDate)
    : "Chưa có";
  const countdown = formatCountdown(caseItem, now);
  const isOverdue = getAlertLevel(caseItem.dueDate, now) === "Quá hạn";

  return [
    isOverdue
      ? `Hồ sơ ${caseItem.caseCode} đã quá hạn xử lý.`
      : `Hồ sơ ${caseItem.caseCode} sắp đến hạn xử lý.`,
    `Ngày hẹn trả: ${appointmentDate}.`,
    `Thời hạn: ${countdown}.`,
    isOverdue
      ? "Vui lòng kiểm tra tình trạng hồ sơ."
      : "Vui lòng kiểm tra và theo dõi tiến độ hồ sơ.",
  ].join("\n");
}

function ReminderModal({ assignee, caseItem, now, onClose, onSent }) {
  const [channels, setChannels] = useState(["System"]);
  const [message, setMessage] = useState(() => createDefaultReminder(caseItem, now));
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState(null);
  const sendingRef = useRef(false);
  const alertLevel = getAlertLevel(caseItem.dueDate, now);

  function isChannelAvailable(channel) {
    if (channel === "System") return Boolean(caseItem.currentAssigneeId);
    if (channel === "Email") return Boolean(assignee?.email?.trim());
    if (channel === "Zalo") return Boolean(assignee?.phoneNumber?.trim());
    return false;
  }

  function getUnavailableReason(channel) {
    if (channel === "Email") return "Cán bộ xử lý chưa có địa chỉ email.";
    if (channel === "Zalo") return "Cán bộ xử lý chưa có thông tin nhận Zalo.";
    return "Hồ sơ chưa có cán bộ xử lý.";
  }

  function toggleChannel(channel) {
    setError("");
    setResult(null);
    setChannels((current) => current.includes(channel)
      ? current.filter((item) => item !== channel)
      : [...current, channel]);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (sendingRef.current) return;
    if (!channels.length) {
      setError("Vui lòng chọn ít nhất một kênh gửi nhắc nhở.");
      return;
    }

    sendingRef.current = true;
    setSending(true);
    setError("");
    setResult(null);
    try {
      const response = await sendReminder({
        caseId: caseItem.id,
        message: message.trim(),
        channels,
      });
      setResult(response.data ?? {});
      if (response.data?.system?.success) {
        notifyNotificationsUpdated();
      }
      onSent(response);
    } catch (sendError) {
      setError(sendError.message || "Không thể gửi nhắc nhở.");
    } finally {
      sendingRef.current = false;
      setSending(false);
    }
  }

  return (
    <AlertModal onClose={sending ? () => {} : onClose} title="Gửi nhắc nhở">
      <form onSubmit={handleSubmit}>
        <div className="case-modal__body reminder-form">
          <dl className="reminder-summary">
            <div className="reminder-summary__column">
              <div><dt>Hồ sơ</dt><dd>{caseItem.caseCode}</dd></div>
              <div><dt>Ngày tiếp nhận</dt><dd>{caseItem.receivedAt ? formatDateTime(caseItem.receivedAt) : "Chưa có dữ liệu"}</dd></div>
              <div><dt>Ngày hẹn trả</dt><dd>{caseItem.appointmentReturnDate ? formatDateTime(caseItem.appointmentReturnDate) : "Chưa có dữ liệu"}</dd></div>
              <div><dt>Ngày trả thực tế</dt><dd>{caseItem.completedAt ? formatDateTime(caseItem.completedAt) : "Chưa hoàn thành"}</dd></div>
              <div><dt>Thời hạn hiện tại</dt><dd>{formatCountdown(caseItem, now)}</dd></div>
            </div>
            <div className="reminder-summary__column">
              <div><dt>Người xử lý</dt><dd>{caseItem.assigneeName || "Chưa phân công"}</dd></div>
              <div><dt>Email người xử lý</dt><dd>{assignee?.email?.trim() || "Chưa có email"}</dd></div>
              <div><dt>Số điện thoại người xử lý</dt><dd>{assignee?.phoneNumber?.trim() || "Chưa có số điện thoại"}</dd></div>
              <div><dt>Mức cảnh báo</dt><dd><AlertBadge level={alertLevel} /></dd></div>
            </div>
          </dl>

          <fieldset className="reminder-channels">
            <legend>Kênh gửi</legend>
            {reminderChannels.map((channel) => {
              const available = isChannelAvailable(channel.id);
              return (
                <label key={channel.id} title={available ? undefined : getUnavailableReason(channel.id)}>
                  <input checked={channels.includes(channel.id)} disabled={!available || sending} type="checkbox" onChange={() => toggleChannel(channel.id)} />
                  <span>{channel.label}</span>
                  {!available && <small>{getUnavailableReason(channel.id)}</small>}
                </label>
              );
            })}
          </fieldset>

          <label className="reminder-message">
            <span>Nội dung nhắc nhở</span>
            <textarea disabled={sending} maxLength="1000" required rows="4" value={message} onChange={(event) => { setMessage(event.target.value); setResult(null); }} />
          </label>
          {error && <p className="reminder-error" role="alert">{error}</p>}
          {result && (
            <ul className="reminder-results" aria-label="Kết quả gửi nhắc nhở">
              {Object.entries(result).map(([channel, channelResult]) => (
                <li className={channelResult.success ? "is-success" : "is-error"} key={channel}>
                  <strong>{{ system: "Thông báo hệ thống", email: "Email", zalo: "Zalo" }[channel] ?? channel}:</strong>{" "}
                  {channelResult.success ? "Thành công" : channelResult.message || "Thất bại"}
                </li>
              ))}
            </ul>
          )}
        </div>
        <footer className="case-modal__footer">
          <button className="cases-button cases-button--secondary" disabled={sending} type="button" onClick={onClose}>Đóng</button>
          <button className="cases-button cases-button--primary" disabled={sending || !channels.length || !message.trim()} type="submit"><Send size={14} /> {sending ? "Đang gửi..." : "Gửi"}</button>
        </footer>
      </form>
    </AlertModal>
  );
}

function AlertsPage() {
  const [now, setNow] = useState(() => Date.now());
  const [caseList, setCaseList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [catalogError, setCatalogError] = useState("");
  const [catalogs, setCatalogs] = useState({ fields: [], procedures: [], departments: [], users: [] });
  const [counts, setCounts] = useState({ upcoming: 0, today: 0, overdue: 0 });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [currentPage, setCurrentPage] = useState(1);
  const [draftFilters, setDraftFilters] = useState(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [selectedCase, setSelectedCase] = useState(null);
  const [reminderCase, setReminderCase] = useState(null);
  const [reminderFeedback, setReminderFeedback] = useState("");
  const [lastSync, setLastSync] = useState(null);
  const [syncError, setSyncError] = useState("");
  const [syncLoading, setSyncLoading] = useState(true);

  useEffect(() => {
    const intervalId = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(intervalId);
  }, []);

  useEffect(() => {
    let isCurrent = true;
    Promise.all([getProcedureFields(), getProcedures(), getDepartments(), getUsers()])
      .then(([fields, procedures, departments, users]) => {
        if (!isCurrent) return;
        setCatalogs({
          fields: (fields ?? []).filter((item) => item.isActive !== false),
          procedures: (procedures ?? []).filter((item) => item.isActive !== false),
          departments: (departments ?? []).filter((item) => item.isActive !== false),
          users: (users ?? []).filter((item) => item.isActive !== false),
        });
      })
      .catch((error) => {
        if (isCurrent) setCatalogError(error.message || "Không thể tải dữ liệu bộ lọc.");
      });
    return () => { isCurrent = false; };
  }, []);

  useEffect(() => {
    let isCurrent = true;
    getLastImportSync()
      .then((sync) => { if (isCurrent) setLastSync(sync); })
      .catch((error) => { if (isCurrent) setSyncError(error.message || "Không thể tải thời điểm cập nhật dữ liệu."); })
      .finally(() => { if (isCurrent) setSyncLoading(false); });
    return () => { isCurrent = false; };
  }, []);

  useEffect(() => {
    let isCurrent = true;
    getCaseAlerts({
      type: appliedFilters.alertLevel,
      keyword: appliedFilters.search.trim(),
      procedureFieldId: appliedFilters.fieldId,
      procedureId: appliedFilters.procedureId,
      departmentId: appliedFilters.departmentId,
      assignedUserId: appliedFilters.assignedUserId,
      pageIndex: currentPage,
      pageSize: PAGE_SIZE,
    })
      .then((response) => {
        if (!isCurrent) return;
        setCaseList((response.results ?? []).map(mapApiAlert));
        setTotalCount(response.totalCount ?? 0);
        setTotalPages(Math.max(response.totalPages ?? 0, 1));
        setCounts({
          upcoming: response.upcomingCount ?? 0,
          today: response.dueTodayCount ?? 0,
          overdue: response.overdueCount ?? 0,
        });
      })
      .catch((error) => {
        if (isCurrent) setLoadError(error.message || "Không thể tải dữ liệu cảnh báo.");
      })
      .finally(() => {
        if (isCurrent) setLoading(false);
      });
    return () => { isCurrent = false; };
  }, [appliedFilters, currentPage]);

  const filteredProcedures = catalogs.procedures.filter((item) =>
    !draftFilters.fieldId || String(item.procedureFieldId) === String(draftFilters.fieldId));
  const filteredUsers = catalogs.users.filter((item) =>
    !draftFilters.departmentId || String(item.departmentId) === String(draftFilters.departmentId));
  const totalAlerts = counts.upcoming + counts.today + counts.overdue;
  const kpiCards = [
    { label: "Tổng cảnh báo", value: totalAlerts, icon: BellRing, tone: "blue" },
    { label: "Sắp hạn", value: counts.upcoming, icon: Clock3, tone: "orange" },
    { label: "Đến hạn hôm nay", value: counts.today, icon: CalendarClock, tone: "purple" },
    { label: "Quá hạn", value: counts.overdue, icon: TriangleAlert, tone: "red" },
  ];

  function changeDraftFilter(name, value) {
    setDraftFilters((current) => {
      const next = { ...current, [name]: value };
      if (name === "fieldId" && !catalogs.procedures.some((item) =>
        String(item.id) === String(current.procedureId)
        && String(item.procedureFieldId) === String(value))) next.procedureId = "";
      if (name === "departmentId" && !catalogs.users.some((item) =>
        String(item.id) === String(current.assignedUserId)
        && String(item.departmentId) === String(value))) next.assignedUserId = "";
      return next;
    });
  }

  function applyFilters(event) {
    event.preventDefault();
    setLoading(true);
    setLoadError("");
    setAppliedFilters({ ...draftFilters });
    setCurrentPage(1);
  }

  function resetFilters() {
    setLoading(true);
    setLoadError("");
    setDraftFilters({ ...emptyFilters });
    setAppliedFilters({ ...emptyFilters });
    setCurrentPage(1);
  }

  function changePage(page) {
    setLoading(true);
    setLoadError("");
    setCurrentPage(page);
  }

  function openAlertDetail(caseItem) {
    setSelectedCase(caseItem);
  }

  function openReminder(caseItem) {
    setSelectedCase(null);
    setReminderCase(caseItem);
    setReminderFeedback("");
  }

  function handleReminderSent(response) {
    const channelResults = Object.values(response.data ?? {});
    const allSucceeded = channelResults.length > 0
      && channelResults.every((item) => item.success);
    setReminderFeedback(allSucceeded
      ? "Đã gửi nhắc nhở."
      : "Đã xử lý nhắc nhở. Một số kênh chưa gửi thành công.");
  }

  return (
    <section className="alerts-page">
      <div className="alerts-page__heading"><h1>Cảnh báo hồ sơ</h1><p>Theo dõi hồ sơ sắp hạn, đến hạn và quá hạn xử lý.</p></div>
      <p className="data-sync-status">Cập nhật dữ liệu lần cuối: {syncLoading ? "Đang tải..." : formatLastUpdated(lastSync?.lastUpdatedAt)}</p>
      {syncError && <div className="data-sync-error" role="alert">{syncError}</div>}
      {lastSync?.isStale && (
        <div className="data-stale-warning" role="alert">
          Dữ liệu chưa được cập nhật trong hơn {lastSync.staleDataHours} giờ. Thông tin cảnh báo có thể chưa phản ánh trạng thái mới nhất.
        </div>
      )}

      <div className="alerts-kpi-grid" aria-label="Thống kê cảnh báo">
        {kpiCards.map(({ icon: Icon, label, tone, value }) => (
          <article className={`alerts-kpi alerts-kpi--${tone}`} key={label}><span><strong>{label}</strong><b>{loading ? "—" : value}</b></span><i aria-hidden="true"><Icon size={20} /></i></article>
        ))}
      </div>

      {reminderFeedback && <div className="alerts-feedback" role="status"><BellRing size={15} /> {reminderFeedback}<button aria-label="Đóng thông báo" type="button" onClick={() => setReminderFeedback("")}><X size={14} /></button></div>}

      <form className="cases-filter-card alerts-filter-card" onSubmit={applyFilters}>
        <label><span>Tìm kiếm</span><div className="cases-search-input"><Search size={15} /><input placeholder="Mã hoặc tên hồ sơ" value={draftFilters.search} onChange={(event) => changeDraftFilter("search", event.target.value)} /></div></label>
        <label><span>Lĩnh vực</span><select value={draftFilters.fieldId} onChange={(event) => changeDraftFilter("fieldId", event.target.value)}><option value="">Tất cả lĩnh vực</option>{catalogs.fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Thủ tục hành chính</span><select value={draftFilters.procedureId} onChange={(event) => changeDraftFilter("procedureId", event.target.value)}><option value="">Tất cả thủ tục</option>{filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Phòng ban</span><select value={draftFilters.departmentId} onChange={(event) => changeDraftFilter("departmentId", event.target.value)}><option value="">Tất cả phòng ban</option>{catalogs.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Người xử lý</span><select value={draftFilters.assignedUserId} onChange={(event) => changeDraftFilter("assignedUserId", event.target.value)}><option value="">Tất cả người xử lý</option>{filteredUsers.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
        <label><span>Loại cảnh báo</span><select value={draftFilters.alertLevel} onChange={(event) => changeDraftFilter("alertLevel", event.target.value)}><option value="">Tất cả cảnh báo</option>{alertLevels.map((level) => <option key={level.value} value={level.value}>{level.label}</option>)}</select></label>
        <div className="cases-filter-card__actions"><button className="cases-button cases-button--primary" type="submit"><Filter size={15} /> Lọc</button><button className="cases-button cases-button--secondary" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button></div>
        {catalogError && <p className="case-form-error" role="alert">{catalogError}</p>}
      </form>

      <article className="cases-table-card alerts-table-card">
        <div className="cases-table-card__header"><h2>Danh sách cảnh báo</h2><span>{loading ? "Đang tải..." : `${totalCount} hồ sơ`}</span></div>
        <div className="cases-table-wrap">
          <table className="cases-table alerts-table">
            <thead><tr><th>Mã hồ sơ</th><th>Tên hồ sơ</th><th>Lĩnh vực</th><th>Thủ tục hành chính</th><th>Phòng ban</th><th>Người xử lý</th><th>Hạn xử lý</th><th>Ngày hẹn trả</th><th>Thời hạn</th><th>Mức cảnh báo</th><th>Thao tác</th></tr></thead>
            <tbody>
              {loading && <tr><td className="cases-table__empty" colSpan="11">Đang tải dữ liệu cảnh báo...</td></tr>}
              {!loading && loadError && <tr><td className="cases-table__empty" colSpan="11" role="alert">Lỗi: {loadError}</td></tr>}
              {!loading && !loadError && caseList.map((caseItem) => {
                const alertLevel = getAlertLevel(caseItem.dueDate, now);
                return (
                  <tr className="alerts-table__clickable-row" key={caseItem.id} onClick={() => openAlertDetail(caseItem)}>
                    <td className="cases-table__code">{caseItem.caseCode}</td><td className="cases-table__name">{caseItem.caseName}</td><td>{caseItem.procedureFieldName || "—"}</td><td>{caseItem.procedureName || "—"}</td><td>{caseItem.departmentName || "—"}</td><td>{caseItem.assigneeName || "—"}</td><td>{formatDateTime(caseItem.dueDate)}</td><td>{formatDateTime(caseItem.appointmentReturnDate)}</td>
                    <td className={`alerts-countdown alerts-countdown--${getAlertKey(alertLevel)}`}>{formatCountdown(caseItem, now)}</td><td><AlertBadge level={alertLevel} /></td>
                    <td onClick={(event) => event.stopPropagation()}><div className="cases-row-actions"><button aria-label={`Xem hồ sơ ${caseItem.caseCode}`} className="cases-action-button" title="Xem hồ sơ" type="button" onClick={() => openAlertDetail(caseItem)}><Eye size={14} /></button></div></td>
                  </tr>
                );
              })}
              {!loading && !loadError && !caseList.length && <tr><td className="cases-table__empty" colSpan="11">Không có hồ sơ cảnh báo</td></tr>}
            </tbody>
          </table>
        </div>
        <div className="cases-pagination">
          <span>Trang {currentPage} / {totalPages}</span>
          <div>
            <button aria-label="Trang trước" disabled={loading || currentPage === 1} type="button" onClick={() => changePage(currentPage - 1)}><ChevronLeft size={16} /> Previous</button>
            <button aria-label="Trang sau" disabled={loading || currentPage === totalPages} type="button" onClick={() => changePage(currentPage + 1)}>Next <ChevronRight size={16} /></button>
          </div>
        </div>
      </article>

      {selectedCase && <CaseAlertDetails caseItem={selectedCase} now={now} onClose={() => setSelectedCase(null)} onRemind={openReminder} />}
      {reminderCase && <ReminderModal assignee={catalogs.users.find((item) => item.id === reminderCase.currentAssigneeId)} caseItem={reminderCase} now={now} onClose={() => setReminderCase(null)} onSent={handleReminderSent} />}
    </section>
  );
}

export default AlertsPage;
