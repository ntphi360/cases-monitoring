import { apiFetch } from "./api";

let syncCache = null;
let syncRequest = null;

export function getLastImportSync({ force = false } = {}) {
  if (!force && syncCache && Date.now() - syncCache.loadedAt < 5000) {
    return Promise.resolve(syncCache.data);
  }
  if (!force && syncRequest) return syncRequest;

  syncRequest = apiFetch("/import/last-sync")
    .then((data) => {
      syncCache = { data, loadedAt: Date.now() };
      return data;
    })
    .finally(() => { syncRequest = null; });

  return syncRequest;
}
