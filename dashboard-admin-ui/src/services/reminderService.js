import { apiFetch } from "./api";

export function sendReminder(payload) {
  return apiFetch("/Reminders", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
}
