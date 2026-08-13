import { apiFetch } from "./api";

let previewCache = null;
let previewRequest = null;
let previewUserKey = null;

function buildUserQuery(userId) {
  return userId ? `?userId=${encodeURIComponent(userId)}` : "";
}

export function getNotifications(params = {}) {
  const query = new URLSearchParams();
  ["userId", "isRead", "pageIndex", "pageSize"].forEach((key) => {
    const value = params[key];
    if (value !== undefined && value !== null && value !== "") query.set(key, value);
  });
  const queryString = query.toString();
  return apiFetch(`/Notifications${queryString ? `?${queryString}` : ""}`);
}

export function getNotificationPreview(userId, { force = false } = {}) {
  const userKey = userId ?? "all";
  const cacheIsFresh = previewCache
    && previewUserKey === userKey
    && Date.now() - previewCache.loadedAt < 5000;

  if (!force && cacheIsFresh) return Promise.resolve(previewCache.data);
  if (!force && previewRequest && previewUserKey === userKey) return previewRequest;

  previewUserKey = userKey;
  previewRequest = getNotifications({ userId, pageIndex: 1, pageSize: 5 })
    .then((data) => {
      previewCache = { data, loadedAt: Date.now() };
      return data;
    })
    .finally(() => {
      previewRequest = null;
    });

  return previewRequest;
}

export function markNotificationRead(id) {
  return apiFetch(`/Notifications/${id}/read`, { method: "PUT" });
}

export function markAllNotificationsRead(userId) {
  return apiFetch(`/Notifications/read-all${buildUserQuery(userId)}`, { method: "PUT" });
}
