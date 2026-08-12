import { apiFetch } from "./api";

export function getCaseAlerts(params = {}) {
  const query = new URLSearchParams();
  const supportedParams = [
    "type",
    "keyword",
    "procedureFieldId",
    "procedureId",
    "departmentId",
    "assignedUserId",
    "pageIndex",
    "pageSize",
  ];

  supportedParams.forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") {
      query.set(key, value);
    }
  });

  const queryString = query.toString();
  return apiFetch(`/Cases/alerts${queryString ? `?${queryString}` : ""}`);
}
