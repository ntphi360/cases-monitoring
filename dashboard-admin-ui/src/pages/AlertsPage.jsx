import { useEffect, useState } from "react";
import { BellRing, CalendarClock, ChevronLeft, ChevronRight, Clock3, Eye, Filter, RotateCcw, Search, Send, TriangleAlert, X } from "lucide-react";
import { useSelector } from "react-redux";

import { getCaseAlerts } from "../services/alertService";
import {
  getDepartments,
  getProcedureFields,
  getProcedures,
  getUsers,
} from "../services/caseService";
import "./CasesPage.css";
import "./AlertsPage.css";

const PAGE_SIZE = 10;
const alertLevels = [
  { label: "Sắp hạn", value: "Upcoming" },
  { label: "Đến hạn hôm nay", value: "DueToday" },
  { label: "Quá hạn", value: "Overdue" },
];
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

function mapApiAlert(item) {
  return {
    id: item.id,
    caseCode: item.externalCaseCode,
    caseName: item.applicantName,
    procedureFieldName: item.procedureFieldName,
    procedureName: item.procedureName,
    departmentName: item.departmentName,
    assigneeName: item.assigneeName,
    dueDate: item.deadline,
    appointmentReturnDate: item.appointmentDate,
  };
}

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time = "00:00"] = value.replace(" ", "T").split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year} ${time.slice(0, 5)}`;
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

function formatCountdown(deadline, now) {
  if (!deadline) return "—";
  const difference = new Date(deadline).getTime() - now;
  if (Number.isNaN(difference)) return "—";

  const totalMinutes = Math.floor(Math.abs(difference) / 60000);
  const days = Math.floor(totalMinutes / 1440);
  const hours = Math.floor((totalMinutes % 1440) / 60);
  const minutes = totalMinutes % 60;
  const duration = days > 0
    ? `${days} ngày ${hours} giờ`
    : hours > 0
      ? `${hours} giờ ${minutes} phút`
      : `${minutes} phút`;

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
    ["Thời hạn", formatCountdown(caseItem.dueDate, now)],
  ];

  return (
    <AlertModal onClose={onClose} title="Thông tin hồ sơ cảnh báo">
      <div className="case-modal__body">
        <dl className="case-details">
          {details.map(([label, value]) => <div className="case-details__item" key={label}><dt>{label}</dt><dd>{value || "—"}</dd></div>)}
          <div className="case-details__item"><dt>Mức cảnh báo</dt><dd><AlertBadge level={alertLevel} /></dd></div>
        </dl>
      </div>
      <footer className="case-modal__footer"><button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Đóng</button></footer>
    </AlertModal>
  );
}

function createDefaultMessage(caseItem, now) {
  const countdown = formatCountdown(caseItem.dueDate, now);
  const deadline = formatDateTime(caseItem.dueDate);
  const overdue = countdown.startsWith("Quá hạn");
  return overdue
    ? `Hồ sơ ${caseItem.caseCode} - ${caseItem.caseName} có hạn xử lý ${deadline} và hiện đã ${countdown.toLocaleLowerCase("vi")}. Vui lòng kiểm tra và xử lý hồ sơ.`
    : `Hồ sơ ${caseItem.caseCode} - ${caseItem.caseName} có hạn xử lý ${deadline}. Hiện ${countdown.toLocaleLowerCase("vi")} trước hạn xử lý. Vui lòng kiểm tra và xử lý hồ sơ đúng tiến độ.`;
}

function ReminderModal({ caseItem, now, onClose, onSend }) {
  const [selectedChannels, setSelectedChannels] = useState([]);
  const [message, setMessage] = useState(() => createDefaultMessage(caseItem, now));
  const [error, setError] = useState("");
  const alertLevel = getAlertLevel(caseItem.dueDate, now);
  const allSelected = selectedChannels.length === channels.length;
  const details = [
    ["Mã hồ sơ", caseItem.caseCode],
    ["Tên hồ sơ", caseItem.caseName],
    ["Lĩnh vực", caseItem.procedureFieldName],
    ["Thủ tục hành chính", caseItem.procedureName],
    ["Người xử lý", caseItem.assigneeName],
    ["Phòng ban", caseItem.departmentName],
    ["Hạn xử lý", formatDateTime(caseItem.dueDate)],
    ["Ngày hẹn trả", formatDateTime(caseItem.appointmentReturnDate)],
    ["Thời hạn", formatCountdown(caseItem.dueDate, now)],
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

    onSend({ channels: selectedChannels });
  }

  return (
    <AlertModal onClose={onClose} title="Gửi nhắc nhở">
      <form onSubmit={handleSubmit}>
        <div className="case-modal__body reminder-form">
          <dl className="reminder-details">
            {details.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || "—"}</dd></div>)}
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
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const intervalId = window.setInterval(() => setNow(Date.now()), 60000);
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

  function sendReminder(reminder) {
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
        <label><span>Lĩnh vực</span><select value={draftFilters.fieldId} onChange={(event) => changeDraftFilter("fieldId", event.target.value)}><option value="">Tất cả lĩnh vực</option>{catalogs.fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Thủ tục hành chính</span><select value={draftFilters.procedureId} onChange={(event) => changeDraftFilter("procedureId", event.target.value)}><option value="">Tất cả thủ tục</option>{filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Phòng ban</span><select value={draftFilters.departmentId} onChange={(event) => changeDraftFilter("departmentId", event.target.value)}><option value="">Tất cả phòng ban</option>{catalogs.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Người xử lý</span><select value={draftFilters.assignedUserId} onChange={(event) => changeDraftFilter("assignedUserId", event.target.value)}><option value="">Tất cả người xử lý</option>{filteredUsers.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
        <label><span>Loại cảnh báo</span><select value={draftFilters.alertLevel} onChange={(event) => changeDraftFilter("alertLevel", event.target.value)}><option value="">Tất cả cảnh báo</option>{alertLevels.map((level) => <option key={level.value} value={level.value}>{level.label}</option>)}</select></label>
        <div className="cases-filter-card__actions"><button className="cases-button cases-button--primary" type="submit"><Filter size={15} /> Lọc</button><button className="cases-button cases-button--secondary" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button></div>
        {catalogError && <p className="case-form-error" role="alert">{catalogError}</p>}
      </form>

      <article className="cases-table-card alerts-table-card">
        <div className="cases-table-card__header"><h2>Danh sách cảnh báo</h2><span>{totalCount} hồ sơ</span></div>
        <div className="cases-table-wrap">
          <table className="cases-table alerts-table">
            <thead><tr><th>Mã hồ sơ</th><th>Tên hồ sơ</th><th>Lĩnh vực</th><th>Thủ tục hành chính</th><th>Phòng ban</th><th>Người xử lý</th><th>Hạn xử lý</th><th>Ngày hẹn trả</th><th>Thời hạn</th><th>Mức cảnh báo</th><th>Thao tác</th></tr></thead>
            <tbody>
              {loading && <tr><td className="cases-table__empty" colSpan="11">Đang tải dữ liệu cảnh báo...</td></tr>}
              {!loading && loadError && <tr><td className="cases-table__empty" colSpan="11" role="alert">Lỗi: {loadError}</td></tr>}
              {!loading && !loadError && caseList.map((caseItem) => {
                const alertLevel = getAlertLevel(caseItem.dueDate, now);
                return (
                  <tr key={caseItem.id}>
                    <td className="cases-table__code">{caseItem.caseCode}</td><td className="cases-table__name">{caseItem.caseName}</td><td>{caseItem.procedureFieldName || "—"}</td><td>{caseItem.procedureName || "—"}</td><td>{caseItem.departmentName || "—"}</td><td>{caseItem.assigneeName || "—"}</td><td>{formatDateTime(caseItem.dueDate)}</td><td>{formatDateTime(caseItem.appointmentReturnDate)}</td>
                    <td className={`alerts-countdown alerts-countdown--${getAlertKey(alertLevel)}`}>{formatCountdown(caseItem.dueDate, now)}</td><td><AlertBadge level={alertLevel} /></td>
                    <td><div className="cases-row-actions"><button aria-label={`Xem hồ sơ ${caseItem.caseCode}`} className="cases-action-button" title="Xem hồ sơ" type="button" onClick={() => setSelectedCase(caseItem)}><Eye size={14} /></button>{isAdmin && <button aria-label={`Gửi nhắc nhở ${caseItem.caseCode}`} className="cases-action-button alerts-remind-button" title="Gửi nhắc nhở" type="button" onClick={() => setReminderCase(caseItem)}><Send size={14} /></button>}</div></td>
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

      {selectedCase && <CaseAlertDetails caseItem={selectedCase} now={now} onClose={() => setSelectedCase(null)} />}
      {reminderCase && <ReminderModal caseItem={reminderCase} now={now} onClose={() => setReminderCase(null)} onSend={sendReminder} />}
    </section>
  );
}

export default AlertsPage;
