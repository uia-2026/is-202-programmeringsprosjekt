export function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, ch => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    })[ch]);
}

export const normalize = value => String(value ?? "").trim().toLowerCase();

export const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

export function formatDate(value) {
    const date = new Date(value);
    if (!value || Number.isNaN(date.getTime())) return "Not specified";
    return new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short" }).format(date);
}

export function formatLabel(value) {
    if (!value) return "Not specified";
    return String(value).replace(/([a-z])([A-Z])/g, "$1 $2").replace(/_/g, " ");
}

// The server sends "Need" / "Resource"; compare case-insensitively
export const isNeed = marker => normalize(marker.kind) === "need";
export const isResource = marker => normalize(marker.kind) === "resource";

function inRange(value, min, max) {
    if (value === null || value === undefined || value === "") return false;
    const n = Number(value);
    return Number.isFinite(n) && n >= min && n <= max;
}

export function hasValidCoordinates(marker) {
    return Boolean(marker) && inRange(marker.latitude, -90, 90) && inRange(marker.longitude, -180, 180);
}

export function distanceKm(lat1, lon1, lat2, lon2) {
    const toRad = d => d * Math.PI / 180;
    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);
    const a = Math.sin(dLat / 2) ** 2 +
              Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
    return 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

export function distanceFromPoint(marker, point) {
    if (!point || !hasValidCoordinates(marker)) return null;
    return distanceKm(point.lat, point.lng, Number(marker.latitude), Number(marker.longitude));
}