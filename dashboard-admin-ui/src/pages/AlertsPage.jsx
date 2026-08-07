import { useEffect, useMemo, useState } from "react";
import { BellRing, CalendarClock, Clock3, Eye, Filter, RotateCcw, Search, Send, TriangleAlert, X } from "lucide-react";
import { useSelector } from "react-redux";

import {
  cases,
  departments,
  fields,
  getCaseRelations,
  procedures,
  users,
} from "../data/caseData";
import "./CasesPage.css";
import "./AlertsPage.css";

const alertLevels = ["Sắp hạn", "Đến hạn hôm nay", "Quá hạn"];
const initialNow = Date.now();
const channels = [
  { id: "IN_APP", label: "Thông báo trong hệ thống" },
  { id: "EMAIL", label: "Email" },
  { id: "ZALO", label: "Zalo" },
];

const emptyFilters = {
  search: "",
  fieldId: "",
  procedureId: "",
  departmentId: "",
  assignedUserId: "",
  alertLevel: "",
};

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time = "00:00:00"] = value.split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year} ${time}`;
}

function getAlertLevel(appointmentReturnDate, now) {
  const due = new Date(appointmentReturnDate);
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
  }[level];
}

function formatCountdown(appointmentReturnDate, now) {
  const difference = new Date(appointmentReturnDate).getTime() - now;
  const totalSeconds = Math.floor(Math.abs(difference) / 1000);
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const clock = [hours, minutes, seconds].map((value) => String(value).padStart(2, "0")).join(":");
  const duration = days > 0 ? `${days} ngày ${clock}` : clock;

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

function CaseAlertDetails({ caseItem, now, onClose }) {
  const { department, field, procedure, user } = getCaseRelations(caseItem);
  const alertLevel = getAlertLevel(caseItem.appointmentReturnDate, now);
  const details = [
    ["Mã hồ sơ", caseItem.caseCode],
    ["Tên hồ sơ", caseItem.caseName],
    ["Lĩnh vực", field?.name],
    ["Thủ tục hành chính", procedure?.name],
    ["Phòng ban", department?.name],
    ["Người xử lý", user?.fullName],
    ["Hạn xử lý", formatDateTime(caseItem.dueDate)],
    ["Ngày hẹn trả", formatDateTime(caseItem.appointmentReturnDate)],
    ["Thời hạn", formatCountdown(caseItem.appointmentReturnDate, now)],
  ];

  return (
    <AlertModal onClose={onClose} title="Thông tin hồ sơ cảnh báo">
      <div className="case-modal__body">
        <dl className="case-details">
          {details.map(([label, value]) => <div className="case-details__item" key={label}><dt>{label}</dt><dd>{value ?? "—"}</dd></div>)}
          <div className="case-details__item"><dt>Mức cảnh báo</dt><dd><AlertBadge level={alertLevel} /></dd></div>
          <div className="case-details__item case-details__item--wide"><dt>Ghi chú</dt><dd>{caseItem.note || "Không có ghi chú."}</dd></div>
        </dl>
      </div>
      <footer className="case-modal__footer"><button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Đóng</button></footer>
    </AlertModal>
  );
}

function createDefaultMessage(caseItem, now) {
  const countdown = formatCountdown(caseItem.appointmentReturnDate, now);
  const appointmentReturnDate = formatDateTime(caseItem.appointmentReturnDate);
  const overdue = countdown.startsWith("Quá hạn");
  return overdue
    ? `Hồ sơ ${caseItem.caseCode} - ${caseItem.caseName} có ngày hẹn trả ${appointmentReturnDate} và hiện đã ${countdown.toLocaleLowerCase("vi")}. Vui lòng kiểm tra và xử lý hồ sơ.`
    : `Hồ sơ ${caseItem.caseCode} - ${caseItem.caseName} có ngày hẹn trả ${appointmentReturnDate}. Hiện ${countdown.toLocaleLowerCase("vi")} trước ngày hẹn trả. Vui lòng kiểm tra và xử lý hồ sơ đúng tiến độ.`;
}

function ReminderModal({ caseItem, now, onClose, onSend }) {
  const { department, field, procedure, user } = getCaseRelations(caseItem);
  const [selectedChannels, setSelectedChannels] = useState([]);
  const [message, setMessage] = useState(() => createDefaultMessage(caseItem, now));
  const [error, setError] = useState("");
  const alertLevel = getAlertLevel(caseItem.appointmentReturnDate, now);
  const allSelected = selectedChannels.length === channels.length;
  const details = [
    ["Mã hồ sơ", caseItem.caseCode],
    ["Tên hồ sơ", caseItem.caseName],
    ["Lĩnh vực", field?.name],
    ["Thủ tục hành chính", procedure?.name],
    ["Người xử lý", user?.fullName],
    ["Phòng ban", department?.name],
    ["Email", user?.email],
    ["Số điện thoại", user?.phone],
    ["Hạn xử lý", formatDateTime(caseItem.dueDate)],
    ["Ngày hẹn trả", formatDateTime(caseItem.appointmentReturnDate)],
    ["Thời hạn", formatCountdown(caseItem.appointmentReturnDate, now)],
  ];

  function toggleChannel(channelId) {
    setError("");
    setSelectedChannels((current) => current.includes(channelId)
      ? current.filter((item) => item !== channelId)
      : [...current, channelId]);
  }

  function toggleAll() {
    setError("");
    setSelectedChannels(allSelected ? [] : channels.map((item) => item.id));
  }

  function handleSubmit(event) {
    event.preventDefault();
    if (!selectedChannels.length) {
      setError("Vui lòng chọn ít nhất một kênh gửi nhắc nhở.");
      return;
    }

    onSend({
      id: `reminder-${Date.now()}`,
      caseId: caseItem.id,
      userId: caseItem.assignedUserId,
      channels: selectedChannels,
      message: message.trim(),
      createdAt: new Date().toISOString(),
    });
  }

  return (
    <AlertModal onClose={onClose} title="Gửi nhắc nhở">
      <form onSubmit={handleSubmit}>
        <div className="case-modal__body reminder-form">
          <dl className="reminder-details">
            {details.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value ?? "—"}</dd></div>)}
            <div><dt>Mức cảnh báo</dt><dd><AlertBadge level={alertLevel} /></dd></div>
          </dl>

          <fieldset className="reminder-channels">
            <legend>Kênh gửi nhắc nhở</legend>
            <label className="reminder-channels__all"><input checked={allSelected} type="checkbox" onChange={toggleAll} /> Chọn tất cả</label>
            <div>
              {channels.map((channel) => (
                <label key={channel.id}><input checked={selectedChannels.includes(channel.id)} type="checkbox" onChange={() => toggleChannel(channel.id)} /> {channel.label}</label>
              ))}
            </div>
          </fieldset>

          <label className="reminder-message">
            <span>Nội dung nhắc nhở</span>
            <textarea required rows="4" value={message} onChange={(event) => setMessage(event.target.value)} />
          </label>
          {error && <p className="reminder-error" role="alert">{error}</p>}
        </div>
        <footer className="case-modal__footer">
          <button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Hủy</button>
          <button className="cases-button cases-button--primary" type="submit"><Send size={14} /> Gửi nhắc nhở</button>
        </footer>
      </form>
    </AlertModal>
  );
}

function AlertsPage() {
  const currentRole = useSelector((state) => state.auth.user?.role ?? "ADMIN");
  const isAdmin = String(currentRole).toUpperCase() === "ADMIN";
  const [now, setNow] = useState(initialNow);
  const [draftFilters, setDraftFilters] = useState(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [selectedCase, setSelectedCase] = useState(null);
  const [reminderCase, setReminderCase] = useState(null);
  const [, setReminders] = useState([]);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const intervalId = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(intervalId);
  }, []);

  const alertCases = useMemo(() => cases
    .filter((caseItem) => caseItem.status !== "Hoàn thành")
    .map((caseItem) => ({ ...caseItem, alertLevel: getAlertLevel(caseItem.appointmentReturnDate, now) })), [now]);

  const filteredProcedures = procedures.filter((item) => !draftFilters.fieldId || item.fieldId === draftFilters.fieldId);
  const filteredUsers = users.filter((item) => !draftFilters.departmentId || item.departmentId === draftFilters.departmentId);
  const filteredCases = alertCases.filter((caseItem) => {
    const { department, field, procedure, user } = getCaseRelations(caseItem);
    const search = appliedFilters.search.trim().toLocaleLowerCase("vi");
    const matchesSearch = !search || caseItem.caseCode.toLocaleLowerCase("vi").includes(search) || caseItem.caseName.toLocaleLowerCase("vi").includes(search);

    return matchesSearch
      && (!appliedFilters.fieldId || field?.id === appliedFilters.fieldId)
      && (!appliedFilters.procedureId || procedure?.id === appliedFilters.procedureId)
      && (!appliedFilters.departmentId || department?.id === appliedFilters.departmentId)
      && (!appliedFilters.assignedUserId || user?.id === appliedFilters.assignedUserId)
      && (!appliedFilters.alertLevel || caseItem.alertLevel === appliedFilters.alertLevel);
  });

  const alertCounts = alertLevels.reduce((counts, level) => ({
    ...counts,
    [level]: alertCases.filter((caseItem) => caseItem.alertLevel === level).length,
  }), {});

  const kpiCards = [
    { label: "Tổng cảnh báo", value: alertCases.length, icon: BellRing, tone: "blue" },
    { label: "Sắp hạn", value: alertCounts["Sắp hạn"], icon: Clock3, tone: "orange" },
    { label: "Đến hạn hôm nay", value: alertCounts["Đến hạn hôm nay"], icon: CalendarClock, tone: "purple" },
    { label: "Quá hạn", value: alertCounts["Quá hạn"], icon: TriangleAlert, tone: "red" },
  ];

  function changeDraftFilter(name, value) {
    setDraftFilters((current) => {
      const next = { ...current, [name]: value };
      if (name === "fieldId" && !procedures.some((item) => item.id === current.procedureId && item.fieldId === value)) next.procedureId = "";
      if (name === "departmentId" && !users.some((item) => item.id === current.assignedUserId && item.departmentId === value)) next.assignedUserId = "";
      return next;
    });
  }

  function applyFilters(event) {
    event.preventDefault();
    setAppliedFilters(draftFilters);
  }

  function resetFilters() {
    setDraftFilters(emptyFilters);
    setAppliedFilters(emptyFilters);
  }

  function sendReminder(reminder) {
    setReminders((current) => [...current, reminder]);
    const channelNames = channels.filter((item) => reminder.channels.includes(item.id)).map((item) => item.label).join(", ");
    setSuccessMessage(`Đã gửi nhắc nhở hồ sơ ${reminderCase.caseCode} qua ${channelNames}.`);
    setReminderCase(null);
  }

  return (
    <section className="alerts-page">
      <div className="alerts-page__heading"><h1>Cảnh báo hồ sơ</h1><p>Theo dõi hồ sơ sắp hạn, đến hạn và quá hạn xử lý.</p></div>

      <div className="alerts-kpi-grid" aria-label="Thống kê cảnh báo">
        {kpiCards.map(({ icon: Icon, label, tone, value }) => (
          <article className={`alerts-kpi alerts-kpi--${tone}`} key={label}><span><strong>{label}</strong><b>{value}</b></span><i aria-hidden="true"><Icon size={20} /></i></article>
        ))}
      </div>

      {successMessage && <div className="alerts-feedback" role="status"><BellRing size={15} /> {successMessage}<button aria-label="Đóng thông báo" type="button" onClick={() => setSuccessMessage("")}><X size={14} /></button></div>}

      <form className="cases-filter-card alerts-filter-card" onSubmit={applyFilters}>
        <label><span>Tìm kiếm</span><div className="cases-search-input"><Search size={15} /><input placeholder="Mã hoặc tên hồ sơ" value={draftFilters.search} onChange={(event) => changeDraftFilter("search", event.target.value)} /></div></label>
        <label><span>Lĩnh vực</span><select value={draftFilters.fieldId} onChange={(event) => changeDraftFilter("fieldId", event.target.value)}><option value="">Tất cả lĩnh vực</option>{fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Thủ tục hành chính</span><select value={draftFilters.procedureId} onChange={(event) => changeDraftFilter("procedureId", event.target.value)}><option value="">Tất cả thủ tục</option>{filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Phòng ban</span><select value={draftFilters.departmentId} onChange={(event) => changeDraftFilter("departmentId", event.target.value)}><option value="">Tất cả phòng ban</option>{departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Người xử lý</span><select value={draftFilters.assignedUserId} onChange={(event) => changeDraftFilter("assignedUserId", event.target.value)}><option value="">Tất cả người xử lý</option>{filteredUsers.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
        <label><span>Loại cảnh báo</span><select value={draftFilters.alertLevel} onChange={(event) => changeDraftFilter("alertLevel", event.target.value)}><option value="">Tất cả cảnh báo</option>{alertLevels.map((level) => <option key={level} value={level}>{level}</option>)}</select></label>
        <div className="cases-filter-card__actions"><button className="cases-button cases-button--primary" type="submit"><Filter size={15} /> Lọc</button><button className="cases-button cases-button--secondary" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button></div>
      </form>

      <article className="cases-table-card alerts-table-card">
        <div className="cases-table-card__header"><h2>Danh sách cảnh báo</h2><span>{filteredCases.length} hồ sơ</span></div>
        <div className="cases-table-wrap">
          <table className="cases-table alerts-table">
            <thead><tr><th>Mã hồ sơ</th><th>Tên hồ sơ</th><th>Lĩnh vực</th><th>Thủ tục hành chính</th><th>Phòng ban</th><th>Người xử lý</th><th>Hạn xử lý</th><th>Ngày hẹn trả</th><th>Thời hạn</th><th>Mức cảnh báo</th><th>Thao tác</th></tr></thead>
            <tbody>
              {filteredCases.map((caseItem) => {
                const { department, field, procedure, user } = getCaseRelations(caseItem);
                return (
                  <tr key={caseItem.id}>
                    <td className="cases-table__code">{caseItem.caseCode}</td><td className="cases-table__name">{caseItem.caseName}</td><td>{field?.name}</td><td>{procedure?.name}</td><td>{department?.name}</td><td>{user?.fullName}</td><td>{formatDateTime(caseItem.dueDate)}</td><td>{formatDateTime(caseItem.appointmentReturnDate)}</td>
                    <td className={`alerts-countdown alerts-countdown--${getAlertKey(caseItem.alertLevel)}`}>{formatCountdown(caseItem.appointmentReturnDate, now)}</td><td><AlertBadge level={caseItem.alertLevel} /></td>
                    <td><div className="cases-row-actions"><button aria-label={`Xem hồ sơ ${caseItem.caseCode}`} className="cases-action-button" title="Xem hồ sơ" type="button" onClick={() => setSelectedCase(caseItem)}><Eye size={14} /></button>{isAdmin && <button aria-label={`Gửi nhắc nhở ${caseItem.caseCode}`} className="cases-action-button alerts-remind-button" title="Gửi nhắc nhở" type="button" onClick={() => setReminderCase(caseItem)}><Send size={14} /></button>}</div></td>
                  </tr>
                );
              })}
              {!filteredCases.length && <tr><td className="cases-table__empty" colSpan="11">Không tìm thấy cảnh báo phù hợp.</td></tr>}
            </tbody>
          </table>
        </div>
      </article>

      {selectedCase && <CaseAlertDetails caseItem={selectedCase} now={now} onClose={() => setSelectedCase(null)} />}
      {reminderCase && <ReminderModal caseItem={reminderCase} now={now} onClose={() => setReminderCase(null)} onSend={sendReminder} />}
    </section>
  );
}

export default AlertsPage;
