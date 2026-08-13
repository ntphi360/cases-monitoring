import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, Download, Eye, Filter, RotateCcw, Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";

import {
  getCases,
  getDepartments,
  getProcedureFields,
  getProcedures,
  getUsers,
} from "../services/caseService";
import { exportCases } from "../services/caseExportService";
import {
  getCaseStatusBadgeKey,
  getCaseStatusLabel,
  getDeadlineStatusBadgeKey,
  getDeadlineStatusLabel,
} from "../utils/caseLabels";
import "./CasesPage.css";

const PAGE_SIZE = 10;
const emptyFilters = {
  search: "", fieldId: "", procedureId: "", departmentId: "",
  assignedUserId: "", status: "", receivedFrom: "", receivedTo: "",
};

function mapApiCase(item) {
  return {
    id: item.id,
    caseCode: item.externalCaseCode,
    caseName: item.applicantName,
    receivedDate: item.receivedAt,
    appointmentReturnDate: item.appointmentDate,
    status: item.status,
    deadlineStatus: item.deadlineStatus,
    procedureName: item.procedureName,
    procedureFieldName: item.procedureFieldName,
    departmentName: item.departmentName,
    organizationName: item.organizationName,
    assigneeName: item.assigneeName,
  };
}

function formatDate(value) {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-");
  return `${day}/${month}/${year}`;
}

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time = ""] = value.replace(" ", "T").split("T");
  return `${formatDate(date)}${time ? ` ${time.slice(0, 5)}` : ""}`;
}

function StatusBadge({ status }) {
  return <span className={`cases-status-badge cases-status-badge--${getCaseStatusBadgeKey(status)}`}>{getCaseStatusLabel(status)}</span>;
}

function DeadlineBadge({ status }) {
  return <span className={`cases-status-badge cases-status-badge--${getDeadlineStatusBadgeKey(status)}`}>{getDeadlineStatusLabel(status)}</span>;
}

function CasesPage() {
  const navigate = useNavigate();
  const isAdmin = useSelector((state) => state.auth.user?.roles?.includes("ADMIN") ?? false);
  const [caseList, setCaseList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [catalogError, setCatalogError] = useState("");
  const [catalogs, setCatalogs] = useState({ fields: [], procedures: [], departments: [], users: [] });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [draftFilters, setDraftFilters] = useState(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [currentPage, setCurrentPage] = useState(1);
  const [exporting, setExporting] = useState("");
  const [exportError, setExportError] = useState("");

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
      .catch((error) => { if (isCurrent) setCatalogError(error.message || "Không thể tải dữ liệu bộ lọc."); });
    return () => { isCurrent = false; };
  }, []);

  useEffect(() => {
    let isCurrent = true;
    getCases({
      keyword: appliedFilters.search.trim(), procedureFieldId: appliedFilters.fieldId,
      procedureId: appliedFilters.procedureId, departmentId: appliedFilters.departmentId,
      assignedUserId: appliedFilters.assignedUserId, status: appliedFilters.status,
      receivedFrom: appliedFilters.receivedFrom, receivedTo: appliedFilters.receivedTo,
      pageIndex: currentPage, pageSize: PAGE_SIZE,
    })
      .then((response) => {
        if (!isCurrent) return;
        setCaseList((response.results ?? []).map(mapApiCase));
        setTotalCount(response.totalCount ?? 0);
        setTotalPages(Math.max(response.totalPages ?? 0, 1));
      })
      .catch((error) => { if (isCurrent) setLoadError(error.message || "Không thể tải dữ liệu hồ sơ."); })
      .finally(() => { if (isCurrent) setLoading(false); });
    return () => { isCurrent = false; };
  }, [appliedFilters, currentPage]);

  const filteredProcedures = catalogs.procedures.filter((item) => !draftFilters.fieldId
    || String(item.procedureFieldId) === String(draftFilters.fieldId));

  function changeDraftFilter(name, value) {
    setDraftFilters((current) => {
      const next = { ...current, [name]: value };
      if (name === "fieldId" && !catalogs.procedures.some((item) => String(item.id) === String(current.procedureId)
        && String(item.procedureFieldId) === String(value))) next.procedureId = "";
      return next;
    });
  }

  function applyFilters(event) {
    event.preventDefault(); setLoading(true); setLoadError("");
    setAppliedFilters({ ...draftFilters }); setCurrentPage(1);
  }

  function resetFilters() {
    setLoading(true); setLoadError(""); setDraftFilters({ ...emptyFilters });
    setAppliedFilters({ ...emptyFilters }); setCurrentPage(1);
  }

  function changePage(page) { setLoading(true); setLoadError(""); setCurrentPage(page); }

  function openCaseDetail(caseItem) {
    navigate(`/cases/${caseItem.id}`);
  }

  function handleCaseRowKeyDown(event, caseItem) {
    if (event.target === event.currentTarget && event.key === "Enter") {
      openCaseDetail(caseItem);
    }
  }

  async function handleExport(format) {
    setExporting(format); setExportError("");
    try {
      await exportCases({
        keyword: appliedFilters.search.trim(), procedureFieldId: appliedFilters.fieldId,
        procedureId: appliedFilters.procedureId, departmentId: appliedFilters.departmentId,
        assignedUserId: appliedFilters.assignedUserId, status: appliedFilters.status,
        receivedFrom: appliedFilters.receivedFrom, receivedTo: appliedFilters.receivedTo,
      }, format);
    } catch (error) {
      setExportError(error.message || "Không thể export dữ liệu hồ sơ.");
    } finally { setExporting(""); }
  }

  return (
    <section className="cases-page">
      <div className="cases-page__heading">
        <div><h1>Giám sát hồ sơ</h1><p>Theo dõi, tra cứu trạng thái nghiệp vụ và thời hạn hồ sơ.</p></div>
        {isAdmin && <div className="cases-page__heading-actions">
          <button className="cases-button cases-button--secondary" disabled={Boolean(exporting)} type="button" onClick={() => handleExport("xlsx")}><Download size={15} /> {exporting === "xlsx" ? "Đang xuất..." : "Excel"}</button>
          <button className="cases-button cases-button--secondary" disabled={Boolean(exporting)} type="button" onClick={() => handleExport("csv")}><Download size={15} /> CSV</button>
        </div>}
      </div>
      {exportError && <p className="cases-export-error" role="alert">Lỗi: {exportError}</p>}

      <form className="cases-filter-card" onSubmit={applyFilters}>
        <label className="cases-filter-card__search"><span>Tìm kiếm</span><div className="cases-search-input"><Search size={15} aria-hidden="true" /><input placeholder="Mã hoặc tên hồ sơ" value={draftFilters.search} onChange={(event) => changeDraftFilter("search", event.target.value)} /></div></label>
        <label><span>Lĩnh vực</span><select value={draftFilters.fieldId} onChange={(event) => changeDraftFilter("fieldId", event.target.value)}><option value="">Tất cả lĩnh vực</option>{catalogs.fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Thủ tục hành chính</span><select value={draftFilters.procedureId} onChange={(event) => changeDraftFilter("procedureId", event.target.value)}><option value="">Tất cả thủ tục</option>{filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Phòng ban</span><select value={draftFilters.departmentId} onChange={(event) => changeDraftFilter("departmentId", event.target.value)}><option value="">Tất cả phòng ban</option>{catalogs.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Người xử lý</span><select value={draftFilters.assignedUserId} onChange={(event) => changeDraftFilter("assignedUserId", event.target.value)}><option value="">Tất cả người xử lý</option>{catalogs.users.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
        <label><span>Trạng thái nghiệp vụ</span><select value={draftFilters.status} onChange={(event) => changeDraftFilter("status", event.target.value)}><option value="">Tất cả trạng thái</option>{[1, 2, 3, 4, 5, 6].map((value) => <option key={value} value={value}>{getCaseStatusLabel(value)}</option>)}</select></label>
        <label><span>Từ ngày tiếp nhận</span><input type="date" value={draftFilters.receivedFrom} onChange={(event) => changeDraftFilter("receivedFrom", event.target.value)} /></label>
        <label><span>Đến ngày tiếp nhận</span><input min={draftFilters.receivedFrom || undefined} type="date" value={draftFilters.receivedTo} onChange={(event) => changeDraftFilter("receivedTo", event.target.value)} /></label>
        <div className="cases-filter-card__actions"><button className="cases-button cases-button--primary" type="submit"><Filter size={15} /> Lọc</button><button className="cases-button cases-button--secondary" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button></div>
        {catalogError && <p className="case-form-error" role="alert">{catalogError}</p>}
      </form>

      <article className="cases-table-card">
        <div className="cases-table-card__header"><h2>Danh sách hồ sơ</h2><span>{loading ? "Đang tải..." : `${totalCount} hồ sơ`}</span></div>
        <div className="cases-table-wrap"><table className="cases-table">
          <thead><tr><th>Mã hồ sơ</th><th>Tên hồ sơ</th><th>Lĩnh vực</th><th>Thủ tục hành chính</th><th>Phòng ban</th><th>Cơ quan/Đơn vị</th><th>Người xử lý</th><th>Ngày tiếp nhận</th><th>Ngày hẹn trả</th><th>Trạng thái nghiệp vụ</th><th>Tình trạng thời hạn</th><th>Thao tác</th></tr></thead>
          <tbody>
            {loading && <tr><td className="cases-table__empty" colSpan="12">Đang tải dữ liệu hồ sơ...</td></tr>}
            {!loading && loadError && <tr><td className="cases-table__empty" colSpan="12" role="alert">Lỗi: {loadError}</td></tr>}
            {!loading && !loadError && caseList.map((item) => <tr aria-label={`Mở chi tiết hồ sơ ${item.caseCode}`} className="cases-table__clickable-row" key={item.id} role="button" tabIndex={0} onClick={() => openCaseDetail(item)} onKeyDown={(event) => handleCaseRowKeyDown(event, item)}>
              <td className="cases-table__code">{item.caseCode}</td><td className="cases-table__name">{item.caseName}</td><td>{item.procedureFieldName ?? "—"}</td><td>{item.procedureName ?? "—"}</td><td>{item.departmentName ?? "—"}</td><td>{item.organizationName ?? "—"}</td><td>{item.assigneeName ?? "—"}</td><td>{formatDate(item.receivedDate)}</td><td>{formatDateTime(item.appointmentReturnDate)}</td><td><StatusBadge status={item.status} /></td><td><DeadlineBadge status={item.deadlineStatus} /></td><td onClick={(event) => event.stopPropagation()}><div className="cases-row-actions"><button aria-label={`Xem chi tiết ${item.caseCode}`} className="cases-action-button" title="Xem chi tiết" type="button" onClick={() => openCaseDetail(item)}><Eye size={14} /></button></div></td>
            </tr>)}
            {!loading && !loadError && !caseList.length && <tr><td className="cases-table__empty" colSpan="12">Không có hồ sơ phù hợp</td></tr>}
          </tbody>
        </table></div>
        <div className="cases-pagination"><span>Trang {currentPage} / {totalPages}</span><div><button aria-label="Trang trước" disabled={loading || currentPage === 1} type="button" onClick={() => changePage(currentPage - 1)}><ChevronLeft size={16} /> Previous</button><button aria-label="Trang sau" disabled={loading || currentPage === totalPages} type="button" onClick={() => changePage(currentPage + 1)}>Next <ChevronRight size={16} /></button></div></div>
      </article>
    </section>
  );
}

export default CasesPage;
