import { CONFIG } from "./config.js";
import { state, elements } from "./state.js";
import { applyFilters, populateFilterOptions } from "./filters.js";
import { updateCapMessage, updateEmptyMessage } from "./markers.js";
import { clamp, hasValidCoordinates } from "./utils.js";

let debounceTimeout = null;
let requestSequence = 0;

export function scheduleLoad() {
    window.clearTimeout(debounceTimeout);
    debounceTimeout = window.setTimeout(loadMarkers, CONFIG.requestDebounceMs);
}

/** Loads the markers inside the visible map area. */
export async function loadMarkers() {
    const bounds = state.map.getBounds();

    // Zoomed far out, Leaflet reports longitudes beyond ±180 and the server would answer 400
    const params = new URLSearchParams({
        south: clamp(bounds.getSouth(), -90, 90),
        north: clamp(bounds.getNorth(), -90, 90),
        west: clamp(bounds.getWest(), -180, 180),
        east: clamp(bounds.getEast(), -180, 180)
    });

    const request = ++requestSequence;
    state.isLoading = true;
    if (!state.hasLoadedMarkers) updateEmptyMessage(0);

    try {
        const response = await fetch(`${CONFIG.dataUrl}?${params}`, {
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        });
        if (!response.ok) throw new Error(`Map request failed: ${response.status}`);

        const data = await response.json();
        if (request !== requestSequence) return; // a newer pan or zoom replaced this request

        if (!Array.isArray(data.markers)) throw new Error("The map API returned an invalid marker list.");

        state.allMarkers = data.markers.filter(hasValidCoordinates);
        state.hasLoadedMarkers = true;

        updateCapMessage(Boolean(data.truncated));
        populateFilterOptions();
        applyFilters();
    } catch (error) {
        if (request !== requestSequence) return;

        console.error("Unable to load map markers:", error);
        if (!state.hasLoadedMarkers && elements.mapEmptyNote) {
            elements.mapEmptyNote.textContent = "Unable to load map data. Try refreshing the page.";
            elements.mapEmptyNote.style.display = "block";
        }
    } finally {
        if (request === requestSequence) state.isLoading = false;
    }
}