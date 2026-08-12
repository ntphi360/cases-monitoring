import { apiFetch } from "./api";

export async function getCases(params = {}) {
  const query = new URLSearchParams();
  const supportedParams = [
    "keyword",
    "departmentId",
    "procedureFieldId",
    "procedureId",
    "assignedUserId",
    "status",
    "receivedFrom",
    "receivedTo",
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
  return apiFetch(`/Cases/paging${queryString ? `?${queryString}` : ""}`);
}

export function getCaseById(id) {
  return apiFetch(`/Cases/${id}`);
}

export function getCaseAssignments(id) {
  return apiFetch(`/CaseAssignments/by-case/${id}`);
}

export function getCaseHistories(id) {
  return apiFetch(`/CaseHistories/by-case/${id}`);
}

export function getProcedureFields() {
  return apiFetch("/ProcedureFields");
}

export function getProcedures() {
  return apiFetch("/Procedures");
}

export function getDepartments() {
  return apiFetch("/Departments");
}

export function getUsers() {
  return apiFetch("/Users");
}
