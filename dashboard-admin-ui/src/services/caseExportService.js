import { API_BASE_URL } from "../config/api";

export async function exportCases(params = {}, format = "xlsx") {
  const query = new URLSearchParams();
  [
    "keyword",
    "procedureFieldId",
    "procedureId",
    "departmentId",
    "assignedUserId",
    "status",
    "receivedFrom",
    "receivedTo",
  ].forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") query.set(key, value);
  });
  query.set("format", format);

  let response;
  try {
    response = await fetch(`${API_BASE_URL}/Cases/export?${query}`);
  } catch {
    throw new Error("Không thể kết nối máy chủ. Vui lòng thử lại.");
  }
  if (!response.ok) {
    let message = `Export thất bại (${response.status} ${response.statusText})`;
    try {
      const error = await response.json();
      if (error?.message) message = error.message;
    } catch {
      // Giữ message HTTP khi response không phải JSON.
    }
    throw new Error(message);
  }

  const disposition = response.headers.get("content-disposition") ?? "";
  const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  const plainName = disposition.match(/filename="?([^";]+)"?/i)?.[1];
  const fallbackName = `HoSo_${new Date().toISOString().slice(0, 16).replace(/[-T:]/g, "")}.${format}`;
  const fileName = encodedName ? decodeURIComponent(encodedName) : plainName || fallbackName;
  const url = URL.createObjectURL(await response.blob());
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
