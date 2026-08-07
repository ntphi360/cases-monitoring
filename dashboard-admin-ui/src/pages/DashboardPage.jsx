import {
  CircleCheckBig,
  Clock3,
  Files,
  TrendingDown,
  TrendingUp,
  TriangleAlert,
} from "lucide-react";

import {
  CartesianGrid,
  Label,
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
import { Link } from "react-router-dom";

import {
  cases as mockCases,
  getCaseRelations,
} from "../data/caseData";
import "./DashboardPage.css";

const kpiCards = [
  {
    title: "Tổng hồ sơ",
    value: "1,248",
    change: "12% so với tháng trước",
    trend: "up",
    icon: Files,
    tone: "blue",
  },
  {
    title: "Hồ sơ sắp hạn",
    value: "86",
    change: "9% so với tháng trước",
    trend: "down",
    icon: Clock3,
    tone: "orange",
  },
  {
    title: "Hồ sơ quá hạn",
    value: "24",
    change: "6% so với tháng trước",
    trend: "down",
    icon: TriangleAlert,
    tone: "red",
  },
  {
    title: "Đã hoàn thành",
    value: "1,038",
    change: "18% so với tháng trước",
    trend: "up",
    icon: CircleCheckBig,
    tone: "green",
  },
];

const statusData = [
  {
    name: "Đang xử lý",
    value: 218,
    fill: "#2775e8",
  },
  {
    name: "Sắp hạn",
    value: 86,
    fill: "#f59e0b",
  },
  {
    name: "Quá hạn",
    value: 24,
    fill: "#ef4444",
  },
  {
    name: "Hoàn thành",
    value: 1038,
    fill: "#22a667",
  },
];

const timelineData = [
  {
    period: "T1",
    received: 142,
    completed: 112,
  },
  {
    period: "T2",
    received: 178,
    completed: 136,
  },
  {
    period: "T3",
    received: 165,
    completed: 151,
  },
  {
    period: "T4",
    received: 226,
    completed: 184,
  },
  {
    period: "T5",
    received: 198,
    completed: 163,
  },
  {
    period: "T6",
    received: 248,
    completed: 207,
  },
  {
    period: "T7",
    received: 271,
    completed: 234,
  },
];

const dashboardNow = Date.now();

function getAttentionStatus(appointmentReturnDate) {
  const appointmentDate = new Date(appointmentReturnDate);
  const currentDate = new Date(dashboardNow);

  if (appointmentDate.getTime() < dashboardNow) return { label: "Quá hạn", key: "overdue", priority: 0 };
  if (
    appointmentDate.getFullYear() === currentDate.getFullYear()
    && appointmentDate.getMonth() === currentDate.getMonth()
    && appointmentDate.getDate() === currentDate.getDate()
  ) return { label: "Đến hạn hôm nay", key: "today", priority: 1 };
  return { label: "Sắp hạn", key: "upcoming", priority: 2 };
}

const attentionCases = mockCases
  .filter((caseItem) => caseItem.status !== "Hoàn thành")
  .map((caseItem) => ({
    ...caseItem,
    attention: getAttentionStatus(caseItem.appointmentReturnDate),
  }))
  .sort((first, second) => first.attention.priority - second.attention.priority
    || new Date(first.appointmentReturnDate) - new Date(second.appointmentReturnDate))
  .slice(0, 5);

function formatCaseDateTime(value) {
  const [date, time] = value.split("T");
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year} ${time.slice(0, 5)}`;
}

const chartTooltipStyle = {
  border: "1px solid #e4eaf2",
  borderRadius: 8,
  boxShadow: "0 6px 18px rgba(32, 56, 85, 0.1)",
  fontSize: 12,
};

function DonutCenterLabel({ total, viewBox }) {
  const { cx, cy } = viewBox ?? {};

  if (typeof cx !== "number" || typeof cy !== "number") return null;

  return (
    <g className="status-chart__center-label">
      <text className="status-chart__total" x={cx} y={cy - 6} textAnchor="middle">
        {total.toLocaleString("en-US")}
      </text>
      <text className="status-chart__caption" x={cx} y={cy + 12} textAnchor="middle">
        hồ sơ
      </text>
    </g>
  );
}

function DashboardPage() {
  const totalStatusCases = statusData.reduce(
    (total, item) => total + item.value,
    0
  );

  return (
    <section className="dashboard-page">
      <div className="dashboard-page__heading">
        <h1>Dashboard</h1>
        <p>Tổng quan tình hình quản lý hồ sơ trong hệ thống.</p>
      </div>

      <div
        className="kpi-grid"
        aria-label="Thống kê tổng quan hồ sơ"
      >
        {kpiCards.map(
          ({
            title,
            value,
            change,
            trend,
            icon: Icon,
            tone,
          }) => {
            const TrendIcon =
              trend === "up" ? TrendingUp : TrendingDown;

            return (
              <article
                className={`kpi-card kpi-card--${tone}`}
                key={title}
              >
                <div className="kpi-card__top">
                  <span className="kpi-card__title">
                    {title}
                  </span>

                  <span
                    className="kpi-card__icon"
                    aria-hidden="true"
                  >
                    <Icon
                      size={21}
                      strokeWidth={2}
                    />
                  </span>
                </div>

                <strong className="kpi-card__value">
                  {value}
                </strong>

                <div
                  className={`kpi-card__change kpi-card__change--${trend}`}
                >
                  <TrendIcon
                    size={14}
                    strokeWidth={2.2}
                    aria-hidden="true"
                  />

                  <span>{change}</span>
                </div>
              </article>
            );
          }
        )}
      </div>

      <div className="dashboard-charts">
        <article className="dashboard-card dashboard-card--status-chart">
          <div className="dashboard-card__header">
            <div>
              <h2>Thống kê hồ sơ theo trạng thái</h2>
              <p>Phân bổ hồ sơ hiện tại</p>
            </div>
          </div>

          <div
            className="status-chart"
            aria-label="Biểu đồ hồ sơ theo trạng thái"
          >
            <div className="status-chart__plot">
              <ResponsiveContainer
                width="100%"
                height="100%"
              >
                <PieChart>
                  <Pie
                    data={statusData}
                    dataKey="value"
                    nameKey="name"
                    cx="50%"
                    cy="50%"
                    innerRadius="57%"
                    outerRadius="82%"
                    paddingAngle={2}
                    stroke="none"
                    isAnimationActive={false}
                  >
                    <Label
                      content={(labelProps) => (
                        <DonutCenterLabel
                          {...labelProps}
                          total={totalStatusCases}
                        />
                      )}
                    />
                  </Pie>

                  <Tooltip
                    contentStyle={chartTooltipStyle}
                    formatter={(value) => [
                      `${value} hồ sơ`,
                    ]}
                  />

                </PieChart>
              </ResponsiveContainer>
            </div>

            <ul
              className="status-chart__legend"
              aria-label="Chú thích trạng thái hồ sơ"
            >
              {statusData.map((item) => (
                <li
                  key={item.name}
                  style={{ "--legend-color": item.fill }}
                >
                  <span
                    className="status-chart__legend-dot"
                    aria-hidden="true"
                  />
                  <span className="status-chart__legend-content">
                    <strong>{item.name}</strong>
                    <small>
                      {Math.round(
                        (item.value / totalStatusCases) * 100
                      )}% · {item.value.toLocaleString("en-US")}
                    </small>
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </article>

        <article className="dashboard-card dashboard-card--line-chart">
          <div className="dashboard-card__header">
            <div>
              <h2>Hồ sơ theo thời gian</h2>

              <p>
                Tình hình tiếp nhận và hoàn thành gần đây
              </p>
            </div>
          </div>

          <div
            className="timeline-chart"
            aria-label="Biểu đồ hồ sơ theo thời gian"
          >
            <ResponsiveContainer
              width="100%"
              height="100%"
            >
              <LineChart
                data={timelineData}
                margin={{
                  top: 8,
                  right: 8,
                  left: -18,
                  bottom: 0,
                }}
              >
                <CartesianGrid
                  stroke="#e9eef5"
                  strokeDasharray="3 3"
                  vertical={false}
                />

                <XAxis
                  axisLine={false}
                  dataKey="period"
                  tick={{
                    fill: "#7b899d",
                    fontSize: 11,
                  }}
                  tickLine={false}
                  tickMargin={10}
                />

                <YAxis
                  axisLine={false}
                  tick={{
                    fill: "#7b899d",
                    fontSize: 11,
                  }}
                  tickLine={false}
                  tickMargin={8}
                />

                <Tooltip
                  contentStyle={chartTooltipStyle}
                />

                <Legend
                  align="center"
                  iconSize={9}
                  iconType="circle"
                  verticalAlign="bottom"
                  wrapperStyle={{
                    paddingTop: 8,
                    fontSize: 10,
                  }}
                />

                <Line
                  activeDot={{ r: 5 }}
                  dataKey="received"
                  dot={{ r: 3 }}
                  name="Hồ sơ tiếp nhận"
                  stroke="#2775e8"
                  strokeWidth={2.2}
                  type="monotone"
                  isAnimationActive={false}
                />

                <Line
                  activeDot={{ r: 5 }}
                  dataKey="completed"
                  dot={{ r: 3 }}
                  name="Hồ sơ hoàn thành"
                  stroke="#22a667"
                  strokeWidth={2.2}
                  type="monotone"
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </article>
      </div>

      <article className="dashboard-card records-card">
        <div className="dashboard-card__header">
          <div>
            <h2>Hồ sơ cần chú ý</h2>
          </div>

          <Link className="records-card__link" to="/cases">
            Xem tất cả
          </Link>
        </div>

        <div className="records-table-wrap">
          <table className="records-table">
            <thead>
              <tr>
                <th scope="col">Mã hồ sơ</th>
                <th scope="col">Tên hồ sơ</th>
                <th scope="col">Lĩnh vực</th>
                <th scope="col">Ngày hẹn trả</th>
                <th scope="col">Người xử lý</th>
                <th scope="col">Trạng thái</th>
              </tr>
            </thead>

            <tbody>
              {attentionCases.map((caseItem) => {
                const { field, user } = getCaseRelations(caseItem);

                return (
                  <tr key={caseItem.id}>
                    <td className="records-table__code">{caseItem.caseCode}</td>
                    <td>{caseItem.caseName}</td>
                    <td>{field?.name ?? "—"}</td>
                    <td>{formatCaseDateTime(caseItem.appointmentReturnDate)}</td>
                    <td>{user?.fullName ?? "—"}</td>
                    <td>
                      <span className={`status-badge status-badge--${caseItem.attention.key}`}>
                        {caseItem.attention.label}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </article>
    </section>
  );
}

export default DashboardPage;
