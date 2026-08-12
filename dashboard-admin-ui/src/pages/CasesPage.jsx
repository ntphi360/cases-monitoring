import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, Eye, Filter, Pencil, Plus, RotateCcw, Search, Trash2, X } from "lucide-react";
import { useSelector } from "react-redux";

import {
  caseHistories as initialCaseHistories,
  caseStatuses,
  departments as mockDepartments,
  fields as mockFields,
  getCaseRelations,
  procedures as mockProcedures,
  statusKeys,
  users as mockUsers,
} from "../data/caseData";
import {
  getCases,
  getDepartments,
  getProcedureFields,
  getProcedures,
  getUsers,
} from "../services/caseService";
import "./CasesPage.css";

const PAGE_SIZE = 10;

const apiCaseStatuses = {
  1: "Mới tiếp nhận",
  2: "Đang xử lý",
  3: "Chờ xử lý",
  4: "Đã hoàn thành",
  5: "Quá hạn",
  6: "Đã hủy",
};

const apiCaseStatusKeys = {
  "Chờ xử lý": "upcoming",
  "Đã hoàn thành": "completed",
  "Đã hủy": "overdue",
};

function mapApiCase(item) {
  return {
    id: item.id,
    caseCode: item.externalCaseCode,
    caseName: item.applicantName,
    receivedDate: item.receivedAt,
    dueDate: item.deadline,
    appointmentReturnDate: item.appointmentDate,
    completedDate: item.completedAt,
    status: apiCaseStatuses[item.status] ?? "Không xác định",
    processingDays: item.processingDays,
    priority: item.priority,
    currentStepName: item.currentStepName,
    procedureName: item.procedureName,
    procedureFieldName: item.procedureFieldName,
    departmentName: item.departmentName,
    organizationName: item.organizationName,
    assigneeName: item.assigneeName,
    fieldId: "",
    procedureId: "",
    departmentId: "",
    assignedUserId: "",
    note: "",
  };
}

const emptyFilters = {
  search: "",
  fieldId: "",
  procedureId: "",
  departmentId: "",
  assignedUserId: "",
  status: "",
  receivedFrom: "",
  receivedTo: "",
};

const emptyForm = {
  caseCode: "",
  caseName: "",
  fieldId: "",
  procedureId: "",
  departmentId: "",
  assignedUserId: "",
  receivedDate: "",
  dueDate: "",
  appointmentReturnDate: "",
  status: "Mới tiếp nhận",
  note: "",
};

function formatDate(value) {
  if (!value) return "—";
  const [year, month, day] = value.slice(0, 10).split("-");
  return `${day}/${month}/${year}`;
}

function formatDateTime(value) {
  if (!value) return "—";
  const [date, time] = value.split("T");
  return `${formatDate(date)} ${time}`;
}

function normalizeDateTime(value) {
  return value.length === 16 ? `${value}:00` : value;
}

function StatusBadge({ status }) {
  return (
    <span className={`cases-status-badge cases-status-badge--${statusKeys[status] ?? apiCaseStatusKeys[status] ?? "new"}`}>
      {status}
    </span>
  );
}

function CaseModal({ children, onClose, title }) {
  return (
    <div className="case-modal" role="presentation" onMouseDown={onClose}>
      <section
        aria-labelledby="case-modal-title"
        aria-modal="true"
        className="case-modal__dialog"
        role="dialog"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="case-modal__header">
          <h2 id="case-modal-title">{title}</h2>
          <button aria-label="Đóng" className="case-icon-button" type="button" onClick={onClose}>
            <X size={18} />
          </button>
        </header>
        {children}
      </section>
    </div>
  );
}

function CaseDetails({ caseItem, histories, onClose }) {
  const { department, field, procedure, user } = getCaseRelations(caseItem);
  const details = [
    ["Mã hồ sơ", caseItem.caseCode],
    ["Tên hồ sơ", caseItem.caseName],
    ["Cơ quan/đơn vị", caseItem.organizationName],
    ["Lĩnh vực", caseItem.procedureFieldName ?? field?.name],
    ["Thủ tục hành chính", caseItem.procedureName ?? procedure?.name],
    ["Phòng ban xử lý", caseItem.departmentName ?? department?.name],
    ["Người xử lý", caseItem.assigneeName ?? user?.fullName],
    ["Ngày tiếp nhận", formatDate(caseItem.receivedDate)],
    ["Ngày hẹn trả", formatDateTime(caseItem.appointmentReturnDate)],
    ["Ngày hoàn tất", formatDate(caseItem.completedDate)],
  ];

  return (
    <CaseModal onClose={onClose} title="Chi tiết hồ sơ">
      <div className="case-modal__body">
        <dl className="case-details">
          {details.map(([label, value]) => (
            <div className={`case-details__item${label === "Ngày hẹn trả" ? " case-details__item--appointment" : ""}`} key={label}>
              <dt>{label}</dt>
              <dd>{value || "—"}</dd>
            </div>
          ))}
          <div className="case-details__item">
            <dt>Trạng thái</dt>
            <dd><StatusBadge status={caseItem.status} /></dd>
          </div>
          <div className="case-details__item case-details__item--wide">
            <dt>Ghi chú</dt>
            <dd>{caseItem.note || "Không có ghi chú."}</dd>
          </div>
        </dl>

        <section className="case-history">
          <h3>Lịch sử xử lý</h3>
          <ol>
            {histories.map((item) => (
              <li key={item.id}>
                <span className="case-history__dot" aria-hidden="true" />
                <div>
                  <strong>{item.status}</strong>
                  <time>{formatDateTime(item.createdAt)}</time>
                  <small>{item.note}</small>
                </div>
              </li>
            ))}
          </ol>
        </section>
      </div>
      <footer className="case-modal__footer">
        <button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Đóng</button>
      </footer>
    </CaseModal>
  );
}

function getCaseFormValues(caseItem) {
  if (!caseItem) return emptyForm;
  const { department, field } = getCaseRelations(caseItem);

  return {
    caseCode: caseItem.caseCode,
    caseName: caseItem.caseName,
    fieldId: field?.id ?? "",
    procedureId: caseItem.procedureId,
    departmentId: department?.id ?? "",
    assignedUserId: caseItem.assignedUserId,
    receivedDate: caseItem.receivedDate,
    dueDate: caseItem.dueDate.slice(0, 10),
    appointmentReturnDate: caseItem.appointmentReturnDate?.slice(0, 16) ?? "",
    status: caseItem.status,
    note: caseItem.note,
  };
}

function CaseForm({ existingCases, initialCase, onClose, onSave }) {
  const [form, setForm] = useState(() => getCaseFormValues(initialCase));
  const [error, setError] = useState("");
  const availableProcedures = mockProcedures.filter((item) => item.fieldId === form.fieldId);
  const availableUsers = mockUsers.filter((item) => item.departmentId === form.departmentId);

  function updateForm(name, value) {
    setError("");
    setForm((current) => ({ ...current, [name]: value }));
  }

  function handleFieldChange(fieldId) {
    const selectedField = mockFields.find((item) => item.id === fieldId);
    setError("");
    setForm((current) => ({
      ...current,
      fieldId,
      procedureId: "",
      departmentId: selectedField?.departmentId ?? "",
      assignedUserId: "",
    }));
  }

  function handleSubmit(event) {
    event.preventDefault();
    const submittedAppointmentReturnDate = event.currentTarget.elements.appointmentReturnDate.value;
    const procedure = mockProcedures.find((item) => item.id === form.procedureId);
    const field = mockFields.find((item) => item.id === form.fieldId);
    const user = mockUsers.find((item) => item.id === form.assignedUserId);

    if (existingCases.some((item) => item.id !== initialCase?.id && item.caseCode.toLowerCase() === form.caseCode.trim().toLowerCase())) {
      setError("Mã hồ sơ đã tồn tại.");
      return;
    }
    if (procedure?.fieldId !== field?.id || field?.departmentId !== form.departmentId || user?.departmentId !== form.departmentId) {
      setError("Thông tin lĩnh vực, thủ tục, phòng ban hoặc người xử lý không hợp lệ.");
      return;
    }

    onSave({
      id: initialCase?.id ?? `case-${Date.now()}`,
      caseCode: form.caseCode.trim(),
      caseName: form.caseName.trim(),
      procedureId: form.procedureId,
      assignedUserId: form.assignedUserId,
      receivedDate: form.receivedDate,
      dueDate: `${form.dueDate}T17:00:00`,
      appointmentReturnDate: normalizeDateTime(submittedAppointmentReturnDate),
      completedDate: form.status === "Hoàn thành"
        ? initialCase?.completedDate || new Date().toISOString().slice(0, 10)
        : "",
      status: form.status,
      note: form.note.trim(),
    });
  }

  return (
    <CaseModal onClose={onClose} title={initialCase ? "Chỉnh sửa hồ sơ" : "Thêm hồ sơ"}>
      <form onSubmit={handleSubmit}>
        <div className="case-modal__body case-form-grid">
          <label>
            <span>Mã hồ sơ <em>*</em></span>
            <input required value={form.caseCode} onChange={(event) => updateForm("caseCode", event.target.value)} />
          </label>
          <label>
            <span>Tên hồ sơ <em>*</em></span>
            <input required value={form.caseName} onChange={(event) => updateForm("caseName", event.target.value)} />
          </label>
          <label>
            <span>Lĩnh vực <em>*</em></span>
            <select required value={form.fieldId} onChange={(event) => handleFieldChange(event.target.value)}>
              <option value="">Chọn lĩnh vực</option>
              {mockFields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>
          <label>
            <span>Thủ tục hành chính <em>*</em></span>
            <select disabled={!form.fieldId} required value={form.procedureId} onChange={(event) => updateForm("procedureId", event.target.value)}>
              <option value="">Chọn thủ tục</option>
              {availableProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>
          <label>
            <span>Phòng ban <em>*</em></span>
            <select disabled required value={form.departmentId}>
              <option value="">Tự động theo lĩnh vực</option>
              {mockDepartments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>
          <label>
            <span>Người xử lý <em>*</em></span>
            <select disabled={!form.departmentId} required value={form.assignedUserId} onChange={(event) => updateForm("assignedUserId", event.target.value)}>
              <option value="">Chọn người xử lý</option>
              {availableUsers.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}
            </select>
          </label>
          <label>
            <span>Ngày tiếp nhận <em>*</em></span>
            <input required type="date" value={form.receivedDate} onChange={(event) => updateForm("receivedDate", event.target.value)} />
          </label>
          <label>
            <span>Ngày hẹn trả <em>*</em></span>
            <input min={form.receivedDate ? `${form.receivedDate}T00:00` : undefined} name="appointmentReturnDate" required type="datetime-local" value={form.appointmentReturnDate} onChange={(event) => updateForm("appointmentReturnDate", event.target.value)} />
          </label>
          <label>
            <span>Hạn xử lý <em>*</em></span>
            <input min={form.receivedDate} required type="date" value={form.dueDate} onChange={(event) => updateForm("dueDate", event.target.value)} />
          </label>
          <label>
            <span>Trạng thái <em>*</em></span>
            <select required value={form.status} onChange={(event) => updateForm("status", event.target.value)}>
              {caseStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </label>
          <label className="case-form-grid__wide">
            <span>Ghi chú</span>
            <textarea rows="3" value={form.note} onChange={(event) => updateForm("note", event.target.value)} />
          </label>
          {error && <p className="case-form-error case-form-grid__wide" role="alert">{error}</p>}
        </div>
        <footer className="case-modal__footer">
          <button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Hủy</button>
          <button className="cases-button cases-button--primary" type="submit">
            {initialCase ? "Lưu thay đổi" : "Lưu hồ sơ"}
          </button>
        </footer>
      </form>
    </CaseModal>
  );
}

function DeleteCaseConfirm({ caseItem, onClose, onConfirm }) {
  return (
    <CaseModal onClose={onClose} title="Xóa hồ sơ">
      <div className="case-modal__body case-delete-confirm">
        <span className="case-delete-confirm__icon" aria-hidden="true">
          <Trash2 size={21} />
        </span>
        <div>
          <p>Bạn có chắc chắn muốn xóa hồ sơ này?</p>
          <dl>
            <div><dt>Mã hồ sơ</dt><dd>{caseItem.caseCode}</dd></div>
            <div><dt>Tên hồ sơ</dt><dd>{caseItem.caseName}</dd></div>
          </dl>
          <small>Thao tác này chỉ cập nhật mock state trên frontend.</small>
        </div>
      </div>
      <footer className="case-modal__footer">
        <button className="cases-button cases-button--secondary" type="button" onClick={onClose}>Hủy</button>
        <button className="cases-button cases-button--danger" type="button" onClick={onConfirm}>Xác nhận xóa</button>
      </footer>
    </CaseModal>
  );
}

function CasesPage() {
  const currentRole = useSelector((state) => state.auth.user?.role ?? "ADMIN");
  const isAdmin = String(currentRole).toUpperCase() === "ADMIN";
  const [caseList, setCaseList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [catalogError, setCatalogError] = useState("");
  const [catalogs, setCatalogs] = useState({
    fields: [],
    procedures: [],
    departments: [],
    users: [],
  });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [historyList, setHistoryList] = useState(initialCaseHistories);
  const [draftFilters, setDraftFilters] = useState(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedCase, setSelectedCase] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  const [editingCase, setEditingCase] = useState(null);
  const [deletingCase, setDeletingCase] = useState(null);

  useEffect(() => {
    let isCurrent = true;

    Promise.all([
      getProcedureFields(),
      getProcedures(),
      getDepartments(),
      getUsers(),
    ])
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
        if (!isCurrent) return;
        setCatalogError(error.message || "Không thể tải dữ liệu bộ lọc.");
      });

    return () => {
      isCurrent = false;
    };
  }, []);

  useEffect(() => {
    let isCurrent = true;
    setLoading(true);
    setLoadError("");

    getCases({
      keyword: appliedFilters.search.trim(),
      procedureFieldId: appliedFilters.fieldId,
      procedureId: appliedFilters.procedureId,
      departmentId: appliedFilters.departmentId,
      assignedUserId: appliedFilters.assignedUserId,
      status: appliedFilters.status,
      receivedFrom: appliedFilters.receivedFrom,
      receivedTo: appliedFilters.receivedTo,
      pageIndex: currentPage,
      pageSize: PAGE_SIZE,
    })
      .then((response) => {
        if (!isCurrent) return;
        setCaseList((response.results ?? []).map(mapApiCase));
        setTotalCount(response.totalCount ?? 0);
        const pageCount = Math.max(response.totalPages ?? 0, 1);
        setTotalPages(pageCount);
        if (currentPage > pageCount) setCurrentPage(pageCount);
      })
      .catch((error) => {
        if (!isCurrent) return;
        setLoadError(error.message || "Không thể tải dữ liệu hồ sơ.");
      })
      .finally(() => {
        if (isCurrent) setLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, [appliedFilters, currentPage]);

  const filteredProcedures = catalogs.procedures.filter((item) =>
    !draftFilters.fieldId
    || String(item.procedureFieldId) === String(draftFilters.fieldId));
  const pageCases = caseList;

  function changeDraftFilter(name, value) {
    setDraftFilters((current) => {
      const next = { ...current, [name]: value };
      if (name === "fieldId" && !catalogs.procedures.some((item) =>
        String(item.id) === String(current.procedureId)
        && String(item.procedureFieldId) === String(value))) next.procedureId = "";
      return next;
    });
  }

  function applyFilters(event) {
    event.preventDefault();
    setAppliedFilters({ ...draftFilters });
    setCurrentPage(1);
  }

  function resetFilters() {
    setDraftFilters({ ...emptyFilters });
    setAppliedFilters({ ...emptyFilters });
    setCurrentPage(1);
  }

  function addCase(caseItem) {
    setCaseList((current) => [caseItem, ...current]);
    setHistoryList((current) => [
      ...current,
      {
        id: `history-${Date.now()}`,
        caseId: caseItem.id,
        status: "Tiếp nhận hồ sơ",
        createdAt: new Date().toISOString().slice(0, 19),
        note: "Hồ sơ mới được tạo trên hệ thống.",
      },
    ]);
    setAppliedFilters(emptyFilters);
    setDraftFilters(emptyFilters);
    setCurrentPage(1);
    setIsAdding(false);
  }

  function updateCase(updatedCase) {
    const previousCase = caseList.find((item) => item.id === updatedCase.id);
    setCaseList((current) => current.map((item) => item.id === updatedCase.id ? updatedCase : item));

    if (previousCase?.status !== updatedCase.status) {
      setHistoryList((current) => [
        ...current,
        {
          id: `history-${Date.now()}`,
          caseId: updatedCase.id,
          status: updatedCase.status,
          createdAt: new Date().toISOString().slice(0, 19),
          note: `Cập nhật trạng thái từ ${previousCase.status} thành ${updatedCase.status}.`,
        },
      ]);
    }

    setEditingCase(null);
  }

  function deleteCase() {
    const nextTotalCount = Math.max(0, totalCount - 1);
    const nextTotalPages = Math.max(1, Math.ceil(nextTotalCount / PAGE_SIZE));
    setCaseList((current) => current.filter((item) => item.id !== deletingCase.id));
    setHistoryList((current) => current.filter((item) => item.caseId !== deletingCase.id));
    setTotalCount(nextTotalCount);
    setTotalPages(nextTotalPages);
    setCurrentPage((page) => Math.min(page, nextTotalPages));
    setDeletingCase(null);
  }

  return (
    <section className="cases-page">
      <div className="cases-page__heading">
        <div>
          <h1>Quản lý hồ sơ</h1>
          <p>Theo dõi, tra cứu và quản lý hồ sơ hành chính.</p>
        </div>
        {isAdmin && (
          <button className="cases-button cases-button--primary" type="button" onClick={() => setIsAdding(true)}>
            <Plus size={16} /> Thêm hồ sơ
          </button>
        )}
      </div>

      <form className="cases-filter-card" onSubmit={applyFilters}>
        <label className="cases-filter-card__search">
          <span>Tìm kiếm</span>
          <div className="cases-search-input">
            <Search size={15} aria-hidden="true" />
            <input placeholder="Mã hoặc tên hồ sơ" value={draftFilters.search} onChange={(event) => changeDraftFilter("search", event.target.value)} />
          </div>
        </label>
        <label>
          <span>Lĩnh vực</span>
          <select value={draftFilters.fieldId} onChange={(event) => changeDraftFilter("fieldId", event.target.value)}>
            <option value="">Tất cả lĩnh vực</option>
            {catalogs.fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
        </label>
        <label>
          <span>Thủ tục hành chính</span>
          <select value={draftFilters.procedureId} onChange={(event) => changeDraftFilter("procedureId", event.target.value)}>
            <option value="">Tất cả thủ tục</option>
            {filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
        </label>
        <label>
          <span>Phòng ban</span>
          <select value={draftFilters.departmentId} onChange={(event) => changeDraftFilter("departmentId", event.target.value)}>
            <option value="">Tất cả phòng ban</option>
            {catalogs.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
        </label>
        <label>
          <span>Người xử lý</span>
          <select value={draftFilters.assignedUserId} onChange={(event) => changeDraftFilter("assignedUserId", event.target.value)}>
            <option value="">Tất cả người xử lý</option>
            {catalogs.users.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}
          </select>
        </label>
        <label>
          <span>Trạng thái</span>
          <select value={draftFilters.status} onChange={(event) => changeDraftFilter("status", event.target.value)}>
            <option value="">Tất cả trạng thái</option>
            {Object.entries(apiCaseStatuses).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
        <label>
          <span>Từ ngày tiếp nhận</span>
          <input type="date" value={draftFilters.receivedFrom} onChange={(event) => changeDraftFilter("receivedFrom", event.target.value)} />
        </label>
        <label>
          <span>Đến ngày tiếp nhận</span>
          <input min={draftFilters.receivedFrom || undefined} type="date" value={draftFilters.receivedTo} onChange={(event) => changeDraftFilter("receivedTo", event.target.value)} />
        </label>
        <div className="cases-filter-card__actions">
          <button className="cases-button cases-button--primary" type="submit"><Filter size={15} /> Lọc</button>
          <button className="cases-button cases-button--secondary" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button>
        </div>
        {catalogError && <p className="case-form-error" role="alert">{catalogError}</p>}
      </form>

      <article className="cases-table-card">
        <div className="cases-table-card__header">
          <h2>Danh sách hồ sơ</h2>
          <span>{totalCount} hồ sơ</span>
        </div>
        <div className="cases-table-wrap">
          <table className="cases-table">
            <thead>
              <tr>
                <th>Mã hồ sơ</th><th>Tên hồ sơ</th><th>Lĩnh vực</th><th>Thủ tục hành chính</th><th>Phòng ban</th><th>Cơ quan/đơn vị</th><th>Người xử lý</th><th>Ngày tiếp nhận</th><th>Ngày hẹn trả</th><th>Trạng thái</th><th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td className="cases-table__empty" colSpan="11">Đang tải dữ liệu hồ sơ...</td>
                </tr>
              )}
              {!loading && loadError && (
                <tr>
                  <td className="cases-table__empty" colSpan="11" role="alert">Lỗi: {loadError}</td>
                </tr>
              )}
              {pageCases.map((caseItem) => (
                  <tr key={caseItem.id}>
                    <td className="cases-table__code">{caseItem.caseCode}</td>
                    <td className="cases-table__name">{caseItem.caseName}</td>
                    <td>{caseItem.procedureFieldName ?? "—"}</td>
                    <td>{caseItem.procedureName ?? "—"}</td>
                    <td>{caseItem.departmentName ?? "—"}</td>
                    <td>{caseItem.organizationName ?? "—"}</td>
                    <td>{caseItem.assigneeName ?? "—"}</td>
                    <td>{formatDate(caseItem.receivedDate)}</td>
                    <td>{formatDateTime(caseItem.appointmentReturnDate)}</td>
                    <td><StatusBadge status={caseItem.status} /></td>
                    <td>
                      <div className="cases-row-actions">
                        <button aria-label={`Xem chi tiết ${caseItem.caseCode}`} className="cases-action-button" title="Xem chi tiết" type="button" onClick={() => setSelectedCase(caseItem)}><Eye size={14} /></button>
                        <button aria-label={`Chỉnh sửa ${caseItem.caseCode}`} className="cases-action-button" title="Chỉnh sửa" type="button" onClick={() => setEditingCase(caseItem)}><Pencil size={14} /></button>
                        {isAdmin && <button aria-label={`Xóa ${caseItem.caseCode}`} className="cases-action-button cases-action-button--danger" title="Xóa" type="button" onClick={() => setDeletingCase(caseItem)}><Trash2 size={14} /></button>}
                      </div>
                    </td>
                  </tr>
              ))}
              {!loading && !loadError && !pageCases.length && <tr><td className="cases-table__empty" colSpan="11">Không có hồ sơ phù hợp</td></tr>}
            </tbody>
          </table>
        </div>
        <div className="cases-pagination">
          <span>Trang {currentPage} / {totalPages}</span>
          <div>
            <button aria-label="Trang trước" disabled={currentPage === 1} type="button" onClick={() => setCurrentPage((page) => page - 1)}><ChevronLeft size={16} /> Previous</button>
            <button aria-label="Trang sau" disabled={currentPage === totalPages} type="button" onClick={() => setCurrentPage((page) => page + 1)}>Next <ChevronRight size={16} /></button>
          </div>
        </div>
      </article>

      {selectedCase && (
        <CaseDetails
          caseItem={selectedCase}
          histories={historyList.filter((item) => item.caseId === selectedCase.id).sort((a, b) => a.createdAt.localeCompare(b.createdAt))}
          onClose={() => setSelectedCase(null)}
        />
      )}
      {isAdding && <CaseForm existingCases={caseList} onClose={() => setIsAdding(false)} onSave={addCase} />}
      {editingCase && <CaseForm existingCases={caseList} initialCase={editingCase} onClose={() => setEditingCase(null)} onSave={updateCase} />}
      {deletingCase && <DeleteCaseConfirm caseItem={deletingCase} onClose={() => setDeletingCase(null)} onConfirm={deleteCase} />}
    </section>
  );
}

export default CasesPage;
