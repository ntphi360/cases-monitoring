import { ArrowLeft } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";

import {
  getCaseAssignments,
  getCaseById,
  getCaseHistories,
} from "../services/caseService";
import {
  getAssignmentStatusLabel,
  getCaseActionTypeLabel,
  getCaseStatusBadgeKey,
  getCaseStatusLabel,
  getDeadlineStatusBadgeKey,
  getDeadlineStatusLabel,
} from "../utils/caseLabels";
import "./CasesPage.css";
import "./CaseDetailPage.css";

function formatDateTime(value) {
  if (!value) return "—";

  const normalized = String(value).replace(" ", "T");
  const [date, time = ""] = normalized.split("T");
  const [year, month, day] = date.split("-");
  if (!year || !month || !day) return String(value);

  return `${day}/${month}/${year}${time ? ` ${time.slice(0, 5)}` : ""}`;
}

function StatusBadge({ status }) {
  return (
    <span className={`cases-status-badge cases-status-badge--${getCaseStatusBadgeKey(status)}`}>
      {getCaseStatusLabel(status)}
    </span>
  );
}

function DeadlineBadge({ status }) {
  return (
    <span className={`cases-status-badge cases-status-badge--${getDeadlineStatusBadgeKey(status)}`}>
      {getDeadlineStatusLabel(status)}
    </span>
  );
}

function SectionMessage({ children, error = false }) {
  return (
    <p className={`case-detail-message${error ? " case-detail-message--error" : ""}`} role={error ? "alert" : undefined}>
      {children}
    </p>
  );
}

function CaseDetailPage() {
  const { id } = useParams();
  const caseId = Number(id);
  const isValidId = Number.isInteger(caseId) && caseId > 0;
  const [caseDetail, setCaseDetail] = useState(null);
  const [assignments, setAssignments] = useState([]);
  const [histories, setHistories] = useState([]);
  const [caseLoading, setCaseLoading] = useState(isValidId);
  const [assignmentLoading, setAssignmentLoading] = useState(isValidId);
  const [historyLoading, setHistoryLoading] = useState(isValidId);
  const [caseError, setCaseError] = useState(isValidId ? "" : "Không tìm thấy hồ sơ");
  const [assignmentError, setAssignmentError] = useState("");
  const [historyError, setHistoryError] = useState("");

  useEffect(() => {
    if (!isValidId) return undefined;

    let isCurrent = true;

    getCaseById(caseId)
      .then((data) => {
        if (isCurrent) setCaseDetail(data);
      })
      .catch((error) => {
        if (!isCurrent) return;
        setCaseError(
          error?.status === 404
            ? "Không tìm thấy hồ sơ"
            : error?.message || "Không thể tải thông tin hồ sơ.",
        );
      })
      .finally(() => {
        if (isCurrent) setCaseLoading(false);
      });

    getCaseAssignments(caseId)
      .then((data) => {
        if (isCurrent) setAssignments(Array.isArray(data) ? data : []);
      })
      .catch((error) => {
        if (isCurrent) setAssignmentError(error?.message || "Không thể tải lịch sử phân công.");
      })
      .finally(() => {
        if (isCurrent) setAssignmentLoading(false);
      });

    getCaseHistories(caseId)
      .then((data) => {
        if (isCurrent) setHistories(Array.isArray(data) ? data : []);
      })
      .catch((error) => {
        if (isCurrent) setHistoryError(error?.message || "Không thể tải lịch sử xử lý.");
      })
      .finally(() => {
        if (isCurrent) setHistoryLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, [caseId, isValidId]);

  const sortedHistories = useMemo(
    () => [...histories].sort((left, right) => new Date(left.createdAt) - new Date(right.createdAt)),
    [histories],
  );

  if (caseLoading) {
    return (
      <section className="cases-page case-detail-page">
        <SectionMessage>Đang tải thông tin hồ sơ...</SectionMessage>
      </section>
    );
  }

  if (caseError || !caseDetail) {
    return (
      <section className="cases-page case-detail-page">
        <Link className="case-detail-back" to="/cases"><ArrowLeft size={16} /> Quay lại danh sách</Link>
        <SectionMessage error>{caseError || "Không tìm thấy hồ sơ"}</SectionMessage>
      </section>
    );
  }

  const details = [
    ["Mã hồ sơ", caseDetail.externalCaseCode],
    ["Tên/Chủ hồ sơ", caseDetail.applicantName],
    ["Lĩnh vực", caseDetail.procedureFieldName],
    ["Thủ tục hành chính", caseDetail.procedureName],
    ["Phòng ban xử lý", caseDetail.departmentName],
    ["Cơ quan/Đơn vị", caseDetail.organizationName],
    ["Người xử lý hiện tại", caseDetail.assigneeName],
    ["Bước xử lý hiện tại", caseDetail.currentStepName],
    ["Ngày tiếp nhận", formatDateTime(caseDetail.receivedAt)],
    ["Ngày hẹn trả", formatDateTime(caseDetail.appointmentDate)],
    ["Hạn xử lý", formatDateTime(caseDetail.deadline)],
    ["Ngày hoàn tất", formatDateTime(caseDetail.completedAt)],
    ["Số ngày xử lý", caseDetail.processingDays == null ? "—" : `${caseDetail.processingDays} ngày`],
  ];

  return (
    <section className="cases-page case-detail-page">
      <div className="case-detail-page__heading">
        <div>
          <Link className="case-detail-back" to="/cases"><ArrowLeft size={16} /> Quay lại danh sách</Link>
          <h1>Chi tiết hồ sơ</h1>
          <p>{caseDetail.externalCaseCode}</p>
        </div>
        <StatusBadge status={caseDetail.status} />
      </div>

      <article className="case-detail-card">
        <h2>Thông tin chung</h2>
        <dl className="case-details case-detail-grid">
          {details.map(([label, value]) => (
            <div className={`case-details__item${label === "Ngày hẹn trả" ? " case-details__item--appointment" : ""}`} key={label}>
              <dt>{label}</dt>
              <dd>{value || "—"}</dd>
            </div>
          ))}
          <div className="case-details__item">
            <dt>Trạng thái nghiệp vụ</dt>
            <dd><StatusBadge status={caseDetail.status} /></dd>
          </div>
          <div className="case-details__item">
            <dt>Tình trạng thời hạn</dt>
            <dd><DeadlineBadge status={caseDetail.deadlineStatus} /></dd>
          </div>
        </dl>
      </article>

      <article className="case-detail-card">
        <h2>Lịch sử phân công</h2>
        {assignmentLoading ? (
          <SectionMessage>Đang tải lịch sử phân công...</SectionMessage>
        ) : assignmentError ? (
          <SectionMessage error>{assignmentError}</SectionMessage>
        ) : assignments.length === 0 ? (
          <SectionMessage>Chưa có lịch sử phân công</SectionMessage>
        ) : (
          <div className="case-detail-table-wrap">
            <table className="case-detail-table">
              <thead>
                <tr>
                  <th>Người xử lý</th>
                  <th>Người phân công</th>
                  <th>Bước xử lý</th>
                  <th>Thời gian phân công</th>
                  <th>Hạn xử lý</th>
                  <th>Hoàn tất</th>
                  <th>Trạng thái</th>
                  <th>Ghi chú</th>
                </tr>
              </thead>
              <tbody>
                {assignments.map((item) => (
                  <tr key={item.id}>
                    <td>{item.assignedToUserName || "—"}</td>
                    <td>{item.assignedByUserName || "—"}</td>
                    <td>{item.stepName || "—"}</td>
                    <td>{formatDateTime(item.assignedAt)}</td>
                    <td>{formatDateTime(item.dueAt)}</td>
                    <td>{formatDateTime(item.completedAt)}</td>
                    <td>{getAssignmentStatusLabel(item.status)}</td>
                    <td>{item.note || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>

      <article className="case-detail-card">
        <h2>Lịch sử xử lý</h2>
        {historyLoading ? (
          <SectionMessage>Đang tải lịch sử xử lý...</SectionMessage>
        ) : historyError ? (
          <SectionMessage error>{historyError}</SectionMessage>
        ) : sortedHistories.length === 0 ? (
          <SectionMessage>Chưa có lịch sử xử lý</SectionMessage>
        ) : (
          <ol className="case-detail-timeline">
            {sortedHistories.map((item) => (
              <li key={item.id}>
                <span className="case-history__dot" aria-hidden="true" />
                <div>
                  <strong>{getCaseActionTypeLabel(item.actionType)}</strong>
                  <time>{formatDateTime(item.createdAt)}</time>
                  <p>{item.description || "Không có mô tả."}</p>
                  {item.userName && <small>Người thực hiện: {item.userName}</small>}
                  {(item.oldStatus || item.newStatus) && (
                    <small>
                      {item.oldStatus ? getCaseStatusLabel(item.oldStatus) : "—"}
                      {" → "}
                      {item.newStatus ? getCaseStatusLabel(item.newStatus) : "—"}
                    </small>
                  )}
                </div>
              </li>
            ))}
          </ol>
        )}
      </article>
    </section>
  );
}

export default CaseDetailPage;
