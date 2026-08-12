import { apiFetch } from "./api";

export function getDashboardSummary() {
  return apiFetch("/Dashboard/summary");
}
