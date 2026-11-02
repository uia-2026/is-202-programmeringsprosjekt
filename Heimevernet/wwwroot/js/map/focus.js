import { CONFIG } from "./config.js";
import { state } from "./state.js";
import { loadMarkersNow } from "./data.js";
import { openPendingFocus } from "./markers.js";
import { normalize } from "./utils.js";

/** Shows a need or resource from the search box on the map and opens its popup. */
export function focusItem(item) {
    state.pendingFocus = { kind: normalize(item.type), id: Number(item.id) };

    // No animation: the new bounds must be correct immediately, so the right area is loaded
    state.map.setView([item.lat, item.lng], Math.max(state.map.getZoom(), CONFIG.focusZoom), { animate: false });

    openPendingFocus();   // already loaded: opens at once
    loadMarkersNow();     // otherwise the popup opens when the marker arrives
}

/** Reads /Map?focus=need:12&lat=..&lng=.. . Returns a validated request, or null. */
export function readMapRequest(params) {
    const lat = parseFloat(params.get("lat") ?? "");
    const lng = parseFloat(params.get("lng") ?? "");
    if (!(lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)) return null;   // NaN fails every comparison

    const match = /^(need|resource):(\d+)$/.exec(params.get("focus") ?? "");
    return { lat, lng, focus: match ? { kind: match[1], id: Number(match[2]) } : null };
}