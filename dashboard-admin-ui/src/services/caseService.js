import { API_BASE_URL } from "../config/api";

export async function getCases(params = {}) {
  const query = new URLSearchParams();
  const supportedParams = [
    "keyword",
    "departmentId",
    "procedureId",
    "status",
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
  const response = await fetch(
    `${API_BASE_URL}/Cases/paging${queryString ? `?${queryString}` : ""}`,
  );

  if (!response.ok) {
    throw new Error(`Không thể tải danh sách hồ sơ (${response.status} ${response.statusText})`);
  }

  return response.json();
}
