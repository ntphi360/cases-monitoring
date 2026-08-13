import { useEffect, useState } from "react";
import { CircleCheckBig, Clock3, Files, TriangleAlert } from "lucide-react";
import {
  CartesianGrid, Label, Legend, Line, LineChart, Pie, PieChart,
  ResponsiveContainer, Tooltip, XAxis, YAxis,
} from "recharts";
import { Link } from "react-router-dom";

import { getDashboardSummary } from "../services/dashboardService";
import { getLastImportSync } from "../services/importService";
import "./DashboardPage.css";

const statusPresentation = {
  Received: { fill: "#8b6fd6", badge: "new" },
  InProgress: { fill: "#2775e8", badge: "processing" },
  Pending: { fill: "#f59e0b", badge: "upcoming" },
  Completed: { fill: "#22a667", badge: "completed" },
  Overdue: { fill: "#ef4444", badge: "overdue" },
  Cancelled: { fill: "#94a3b8", badge: "cancelled" },
};

const chartTooltipStyle = {
  border: "1px solid #e4eaf2",
  borderRadius: 8,
  boxShadow: "0 6px 18px rgba(32, 56, 85, 0.1)",
  fontSize: 12,
};

function formatCaseDateTime(value) {
  if (!value) return "—";
  const [date, time = "00:00"] = value.replace(" ", "T").split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year} ${time.slice(0, 5)}`;
}

function formatLastUpdated(value) {
  if (!value) return "Chưa có lần import thành công";
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function DonutCenterLabel({ total, viewBox }) {
  const { cx, cy } = viewBox ?? {};
  if (typeof cx !== "number" || typeof cy !== "number") return null;

  return (
    <g className="status-chart__center-label">
      <text className="status-chart__total" x={cx} y={cy - 6} textAnchor="middle">{total.toLocaleString("vi-VN")}</text>
      <text className="status-chart__caption" x={cx} y={cy + 12} textAnchor="middle">hồ sơ</text>
    </g>
  );
}

function DashboardPage() {
  const [dashboard, setDashboard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [lastSync, setLastSync] = useState(null);
  const [syncError, setSyncError] = useState("");
  const [syncLoading, setSyncLoading] = useState(true);

  useEffect(() => {
    let isCurrent = true;
    getDashboardSummary()
      .then((response) => { if (isCurrent) setDashboard(response); })
      .catch((error) => { if (isCurrent) setLoadError(error.message || "Không thể tải dữ liệu Dashboard."); })
      .finally(() => { if (isCurrent) setLoading(false); });
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

  const kpiCards = [
    { title: "Tổng hồ sơ", value: dashboard?.totalCases ?? 0, icon: Files, tone: "blue" },
    { title: "Hồ sơ sắp hạn", value: dashboard?.nearDeadlineCases ?? 0, icon: Clock3, tone: "orange" },
    { title: "Hồ sơ quá hạn", value: dashboard?.overdueCases ?? 0, icon: TriangleAlert, tone: "red" },
    { title: "Đã hoàn thành", value: dashboard?.completedCases ?? 0, icon: CircleCheckBig, tone: "green" },
  ];
  const statusData = (dashboard?.statusDistribution ?? []).map((item) => ({
    ...item,
    name: item.label,
    value: item.count,
    fill: statusPresentation[item.key]?.fill ?? "#94a3b8",
  }));
  const totalStatusCases = statusData.reduce((total, item) => total + item.value, 0);
  const timelineData = dashboard?.trend ?? [];
  const recentCases = dashboard?.recentCases ?? [];
  const statusesByValue = new Map((dashboard?.statusDistribution ?? []).map((item) => [item.status, item]));

  return (
    <section className="dashboard-page">
      <div className="dashboard-page__heading"><h1>Dashboard</h1><p>Tổng quan tình hình quản lý hồ sơ trong hệ thống.</p></div>
      <p className="data-sync-status">Cập nhật dữ liệu lần cuối: {syncLoading ? "Đang tải..." : formatLastUpdated(lastSync?.lastUpdatedAt)}</p>
      {syncError && <div className="dashboard-feedback" role="alert">{syncError}</div>}
      {lastSync?.isStale && (
        <div className="data-stale-warning" role="alert">
          Dữ liệu chưa được cập nhật trong hơn {lastSync.staleDataHours} giờ. Thông tin cảnh báo có thể chưa phản ánh trạng thái mới nhất.
        </div>
      )}
      {loadError && <div className="dashboard-feedback" role="alert">Lỗi: {loadError}</div>}

      <div className="kpi-grid" aria-label="Thống kê tổng quan hồ sơ">
        {kpiCards.map(({ title, value, icon: Icon, tone }) => (
          <article className={`kpi-card kpi-card--${tone}`} key={title}>
            <div className="kpi-card__top">
              <span className="kpi-card__title">{title}</span>
              <span className="kpi-card__icon" aria-hidden="true"><Icon size={21} strokeWidth={2} /></span>
            </div>
            <strong className="kpi-card__value">{loading || loadError ? "—" : value.toLocaleString("vi-VN")}</strong>
            <div className="kpi-card__change"><span>{loading ? "Đang tải dữ liệu..." : loadError ? "Không thể tải dữ liệu" : "Dữ liệu hiện tại"}</span></div>
          </article>
        ))}
      </div>

      <div className="dashboard-charts">
        <article className="dashboard-card dashboard-card--status-chart">
          <div className="dashboard-card__header"><div><h2>Thống kê hồ sơ theo trạng thái</h2><p>Phân bổ hồ sơ hiện tại</p></div></div>
          <div className="status-chart" aria-label="Biểu đồ hồ sơ theo trạng thái">
            <div className="status-chart__plot">
              {statusData.length ? (
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={statusData} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius="57%" outerRadius="82%" paddingAngle={2} stroke="none" isAnimationActive={false}>
                      <Label content={(labelProps) => <DonutCenterLabel {...labelProps} total={totalStatusCases} />} />
                    </Pie>
                    <Tooltip contentStyle={chartTooltipStyle} formatter={(value) => [`${value} hồ sơ`]} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p className="dashboard-empty-chart">{loading ? "Đang tải dữ liệu..." : loadError ? "Không thể tải dữ liệu" : "Không có dữ liệu thống kê"}</p>}
            </div>
            <ul className="status-chart__legend" aria-label="Chú thích trạng thái hồ sơ">
              {statusData.map((item) => (
                <li key={item.key} style={{ "--legend-color": item.fill }}>
                  <span className="status-chart__legend-dot" aria-hidden="true" />
                  <span className="status-chart__legend-content">
                    <strong>{item.name}</strong>
                    <small>{totalStatusCases ? Math.round((item.value / totalStatusCases) * 100) : 0}% · {item.value.toLocaleString("vi-VN")}</small>
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </article>

        <article className="dashboard-card dashboard-card--line-chart">
          <div className="dashboard-card__header"><div><h2>Hồ sơ theo thời gian</h2><p>Tình hình tiếp nhận và hoàn thành trong 7 tháng gần nhất</p></div></div>
          <div className="timeline-chart" aria-label="Biểu đồ hồ sơ theo thời gian">
            {timelineData.length ? (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={timelineData} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                  <CartesianGrid stroke="#e9eef5" strokeDasharray="3 3" vertical={false} />
                  <XAxis axisLine={false} dataKey="period" tick={{ fill: "#7b899d", fontSize: 11 }} tickLine={false} tickMargin={10} />
                  <YAxis axisLine={false} tick={{ fill: "#7b899d", fontSize: 11 }} tickLine={false} tickMargin={8} allowDecimals={false} />
                  <Tooltip contentStyle={chartTooltipStyle} />
                  <Legend align="center" iconSize={9} iconType="circle" verticalAlign="bottom" wrapperStyle={{ paddingTop: 8, fontSize: 10 }} />
                  <Line activeDot={{ r: 5 }} dataKey="received" dot={{ r: 3 }} name="Hồ sơ tiếp nhận" stroke="#2775e8" strokeWidth={2.2} type="monotone" isAnimationActive={false} />
                  <Line activeDot={{ r: 5 }} dataKey="completed" dot={{ r: 3 }} name="Hồ sơ hoàn thành" stroke="#22a667" strokeWidth={2.2} type="monotone" isAnimationActive={false} />
                </LineChart>
              </ResponsiveContainer>
            ) : <p className="dashboard-empty-chart">{loading ? "Đang tải dữ liệu..." : loadError ? "Không thể tải dữ liệu" : "Không có dữ liệu thống kê"}</p>}
          </div>
        </article>
      </div>

      <article className="dashboard-card records-card">
        <div className="dashboard-card__header"><div><h2>Hồ sơ gần đây</h2></div><Link className="records-card__link" to="/cases">Xem tất cả</Link></div>
        <div className="records-table-wrap">
          <table className="records-table">
            <thead><tr><th scope="col">Mã hồ sơ</th><th scope="col">Tên hồ sơ</th><th scope="col">Lĩnh vực</th><th scope="col">Ngày hẹn trả</th><th scope="col">Người xử lý</th><th scope="col">Trạng thái</th></tr></thead>
            <tbody>
              {loading && <tr><td className="records-table__empty" colSpan="6">Đang tải dữ liệu...</td></tr>}
              {!loading && loadError && <tr><td className="records-table__empty" colSpan="6">Không thể tải hồ sơ gần đây.</td></tr>}
              {!loading && !loadError && recentCases.map((caseItem) => {
                const status = statusesByValue.get(caseItem.status);
                const badge = statusPresentation[status?.key]?.badge ?? "cancelled";
                return (
                  <tr key={caseItem.id}>
                    <td className="records-table__code">{caseItem.externalCaseCode}</td>
                    <td>{caseItem.applicantName}</td>
                    <td>{caseItem.procedureFieldName || "—"}</td>
                    <td>{formatCaseDateTime(caseItem.appointmentDate)}</td>
                    <td>{caseItem.assigneeName || "—"}</td>
                    <td><span className={`status-badge status-badge--${badge}`}>{status?.label ?? "Không xác định"}</span></td>
                  </tr>
                );
              })}
              {!loading && !loadError && !recentCases.length && <tr><td className="records-table__empty" colSpan="6">Chưa có hồ sơ</td></tr>}
            </tbody>
          </table>
        </div>
      </article>
    </section>
  );
}

export default DashboardPage;
