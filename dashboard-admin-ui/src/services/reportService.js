import { apiFetch } from "./api";

export function getReportSummary(params = {}) {
  const query = new URLSearchParams();
  const supportedParams = [
    "from",
    "to",
    "procedureFieldId",
    "procedureId",
    "departmentId",
    "assignedUserId",
    "status",
  ];

  supportedParams.forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") {
      query.set(key, value);
    }
  });

  const queryString = query.toString();
  return apiFetch(`/Reports/summary${queryString ? `?${queryString}` : ""}`);
}
