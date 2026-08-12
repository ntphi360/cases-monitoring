import { useEffect, useMemo, useState } from "react";
import { CheckCircle2, Clock3, FileText, Filter, RotateCcw, TriangleAlert } from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import {
  getDepartments,
  getProcedureFields,
  getProcedures,
  getUsers,
} from "../services/caseService";
import { getReportSummary } from "../services/reportService";
import "./ReportsPage.css";

const emptyFilters = {
  from: "",
  to: "",
  procedureFieldId: "",
  procedureId: "",
  departmentId: "",
  assignedUserId: "",
  status: "",
};

const emptyReport = {
  totalCases: 0,
  completedCases: 0,
  processingCases: 0,
  overdueCases: 0,
  trendGranularity: "month",
  byProcedureField: [],
  byProcedure: [],
  byDepartment: [],
  byAssignee: [],
  trend: [],
};

const statuses = [
  [1, "Mới tiếp nhận"],
  [2, "Đang xử lý"],
  [3, "Chờ xử lý"],
  [4, "Đã hoàn thành"],
  [5, "Quá hạn"],
  [6, "Đã hủy"],
];

const pieColors = ["#1768dc", "#199b5d", "#e56f0a", "#7c5ce5", "#df303b", "#3b9ca8"];

function EmptyChart() {
  return <p className="reports-empty">Không có dữ liệu</p>;
}

function ReportTable({ rows }) {
  return (
    <div className="reports-table-wrap">
      <table className="reports-table">
        <thead><tr><th>Tên</th><th>Số hồ sơ</th></tr></thead>
        <tbody>
          {rows.map((item) => (
            <tr key={`${item.id ?? "none"}-${item.name}`}>
              <td>{item.name}</td>
              <td>{item.count.toLocaleString("vi-VN")}</td>
            </tr>
          ))}
          {rows.length === 0 && <tr><td className="reports-table__empty" colSpan="2">Không có dữ liệu</td></tr>}
        </tbody>
      </table>
    </div>
  );
}

function ReportsPage() {
  const [draftFilters, setDraftFilters] = useState(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [catalogs, setCatalogs] = useState({ fields: [], procedures: [], departments: [], users: [] });
  const [report, setReport] = useState(emptyReport);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [catalogError, setCatalogError] = useState("");

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

    return () => {
      isCurrent = false;
    };
  }, []);

  useEffect(() => {
    let isCurrent = true;

    getReportSummary(appliedFilters)
      .then((data) => {
        if (isCurrent) setReport({ ...emptyReport, ...data });
      })
      .catch((error) => {
        if (!isCurrent) return;
        setReport(emptyReport);
        setLoadError(error.message || "Không thể tải dữ liệu báo cáo.");
      })
      .finally(() => {
        if (isCurrent) setLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, [appliedFilters]);

  const filteredProcedures = useMemo(
    () => catalogs.procedures.filter((item) =>
      !draftFilters.procedureFieldId
      || String(item.procedureFieldId) === String(draftFilters.procedureFieldId)),
    [catalogs.procedures, draftFilters.procedureFieldId],
  );

  function updateFilter(name, value) {
    setDraftFilters((current) => {
      const next = { ...current, [name]: value };
      if (name === "procedureFieldId") {
        const procedureStillValid = catalogs.procedures.some((item) =>
          String(item.id) === String(current.procedureId)
          && String(item.procedureFieldId) === String(value));
        if (!procedureStillValid) next.procedureId = "";
      }
      return next;
    });
  }

  function applyFilters(event) {
    event.preventDefault();
    setLoadError("");
    setLoading(true);
    setAppliedFilters({ ...draftFilters });
  }

  function resetFilters() {
    setDraftFilters({ ...emptyFilters });
    setAppliedFilters({ ...emptyFilters });
    setLoadError("");
    setLoading(true);
  }

  const kpis = [
    { label: "Tổng hồ sơ", value: report.totalCases, icon: FileText, tone: "blue" },
    { label: "Đã hoàn thành", value: report.completedCases, icon: CheckCircle2, tone: "green" },
    { label: "Đang xử lý", value: report.processingCases, icon: Clock3, tone: "orange" },
    { label: "Quá hạn", value: report.overdueCases, icon: TriangleAlert, tone: "red" },
  ];

  return (
    <section className="reports-page">
      <div className="reports-page__heading">
        <h1>Thống kê &amp; Báo cáo</h1>
        <p>Tổng hợp dữ liệu hồ sơ theo thời gian và đơn vị xử lý.</p>
      </div>

      <form className="reports-filter" onSubmit={applyFilters}>
        <label><span>Từ ngày</span><input type="date" value={draftFilters.from} onChange={(event) => updateFilter("from", event.target.value)} /></label>
        <label><span>Đến ngày</span><input min={draftFilters.from || undefined} type="date" value={draftFilters.to} onChange={(event) => updateFilter("to", event.target.value)} /></label>
        <label><span>Lĩnh vực</span><select value={draftFilters.procedureFieldId} onChange={(event) => updateFilter("procedureFieldId", event.target.value)}><option value="">Tất cả lĩnh vực</option>{catalogs.fields.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Thủ tục</span><select value={draftFilters.procedureId} onChange={(event) => updateFilter("procedureId", event.target.value)}><option value="">Tất cả thủ tục</option>{filteredProcedures.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Phòng ban</span><select value={draftFilters.departmentId} onChange={(event) => updateFilter("departmentId", event.target.value)}><option value="">Tất cả phòng ban</option>{catalogs.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Người xử lý</span><select value={draftFilters.assignedUserId} onChange={(event) => updateFilter("assignedUserId", event.target.value)}><option value="">Tất cả người xử lý</option>{catalogs.users.map((item) => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
        <label><span>Trạng thái</span><select value={draftFilters.status} onChange={(event) => updateFilter("status", event.target.value)}><option value="">Tất cả trạng thái</option>{statuses.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        <div className="reports-filter__actions">
          <button className="reports-button reports-button--primary" type="submit"><Filter size={15} /> Lọc</button>
          <button className="reports-button" type="button" onClick={resetFilters}><RotateCcw size={15} /> Đặt lại</button>
        </div>
        {catalogError && <p className="reports-filter__error" role="alert">{catalogError}</p>}
      </form>

      {loading && <p className="reports-feedback">Đang tải dữ liệu báo cáo...</p>}
      {!loading && loadError && <p className="reports-feedback reports-feedback--error" role="alert">Lỗi: {loadError}</p>}

      {!loading && !loadError && (
        <>
          <div className="reports-kpi-grid">
            {kpis.map(({ icon: Icon, label, tone, value }) => (
              <article className={`reports-kpi reports-kpi--${tone}`} key={label}>
                <span><Icon size={18} /></span>
                <div><small>{label}</small><strong>{value.toLocaleString("vi-VN")}</strong></div>
              </article>
            ))}
          </div>

          <div className="reports-chart-grid reports-chart-grid--wide">
            <article className="reports-card">
              <header><h2>Xu hướng hồ sơ</h2><p>Theo {report.trendGranularity === "day" ? "ngày" : "tháng"}</p></header>
              <div className="reports-chart">
                {report.trend.length === 0 ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={report.trend} margin={{ top: 12, right: 14, left: -18, bottom: 2 }}>
                      <CartesianGrid stroke="#edf1f6" strokeDasharray="3 3" />
                      <XAxis dataKey="period" fontSize={9} tickLine={false} />
                      <YAxis allowDecimals={false} fontSize={9} tickLine={false} />
                      <Tooltip /><Legend wrapperStyle={{ fontSize: 10 }} />
                      <Line dataKey="receivedCount" name="Tiếp nhận" stroke="#1768dc" strokeWidth={2} type="monotone" />
                      <Line dataKey="completedCount" name="Hoàn thành" stroke="#199b5d" strokeWidth={2} type="monotone" />
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </div>
            </article>

            <article className="reports-card">
              <header><h2>Hồ sơ theo lĩnh vực</h2></header>
              <div className="reports-chart">
                {report.byProcedureField.length === 0 ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={report.byProcedureField} dataKey="count" nameKey="name" innerRadius={45} outerRadius={75} paddingAngle={2}>
                        {report.byProcedureField.map((item, index) => <Cell fill={pieColors[index % pieColors.length]} key={item.id ?? item.name} />)}
                      </Pie>
                      <Tooltip /><Legend wrapperStyle={{ fontSize: 9 }} />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </div>
            </article>
          </div>

          <div className="reports-chart-grid">
            <article className="reports-card">
              <header><h2>Hồ sơ theo phòng ban</h2></header>
              <div className="reports-chart">
                {report.byDepartment.length === 0 ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={report.byDepartment} layout="vertical" margin={{ top: 8, right: 18, left: 20, bottom: 0 }}>
                      <CartesianGrid stroke="#edf1f6" strokeDasharray="3 3" />
                      <XAxis allowDecimals={false} fontSize={9} type="number" />
                      <YAxis dataKey="name" fontSize={9} type="category" width={125} />
                      <Tooltip /><Bar dataKey="count" fill="#1768dc" name="Hồ sơ" radius={[0, 4, 4, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </div>
            </article>
            <article className="reports-card"><header><h2>Hồ sơ theo người xử lý</h2></header><ReportTable rows={report.byAssignee} /></article>
          </div>

          <article className="reports-card reports-procedure-card"><header><h2>Hồ sơ theo thủ tục hành chính</h2></header><ReportTable rows={report.byProcedure} /></article>
        </>
      )}
    </section>
  );
}

export default ReportsPage;
