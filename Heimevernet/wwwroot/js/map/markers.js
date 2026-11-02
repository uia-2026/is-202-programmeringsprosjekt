import { COLORS, PRIORITIES } from "./config.js";
import { state, elements } from "./state.js";
import { distanceFromPoint, escapeHtml, formatDate, formatLabel, isNeed, isResource, normalize, markerKey } from "./utils.js";

/** The need's priority as one of the known levels; anything unexpected is treated as Medium. */
export function getPriority(marker) {
    return PRIORITIES.find(p => normalize(p) === normalize(marker.priority)) ?? "Medium";
}

function getMarkerColor(marker) {
    if (isResource(marker)) {
        return marker.status === "Available" ? COLORS.resourceAvailable : COLORS.resourceBusy;
    }
    return COLORS[getPriority(marker)];
}

function describe(marker) {
    return isResource(marker)
        ? `Resource, ${formatLabel(marker.status)}`
        : `${getPriority(marker)} priority need`;
}

const NEED_SYMBOLS = { Low: "L", Medium: "M", Critical: "!" };

// Needs are diamonds, resources are squares, so the type is readable without colour
function createIcon(marker) {
    const resource = isResource(marker);
    const symbol = resource ? "R" : NEED_SYMBOLS[getPriority(marker)];
    const label = escapeHtml(describe(marker));

    const html = `
        <div class="map-marker-shape ${resource ? "map-marker-resource" : "map-marker-need"}"
             style="--marker-color:${getMarkerColor(marker)}"
             title="${label}" aria-label="${label}">
            <span>${symbol}</span>
        </div>`;

    return L.divIcon({
        className: "map-marker-icon",
        html,
        iconSize: [32, 32],
        iconAnchor: [16, 16],
        popupAnchor: [0, -16]
    });
}

const row = (label, value) =>
    value ? `<p><strong>${label}:</strong> ${escapeHtml(value)}</p>` : "";

const detailsUrl = marker =>
    `/${isNeed(marker) ? "Need" : "Resource"}/Details/${encodeURIComponent(marker.id)}`;

function buildPopupHtml(marker) {
    const km = distanceFromPoint(marker, state.searchLocation);

    return `
        <div class="map-popup">
            <h3>${escapeHtml(marker.title)}</h3>
            <p><span class="map-popup-badge" style="background:${getMarkerColor(marker)}">
                ${escapeHtml(describe(marker))}</span></p>
            ${row("Category", marker.category)}
            ${row("Region", marker.region)}
            ${row("Quantity", String(marker.quantity))}
            ${marker.when ? row(isNeed(marker) ? "Deadline" : "Available from", formatDate(marker.when)) : ""}
            ${km !== null ? row("Distance", `${km.toFixed(1)} km`) : ""}
            <a class="map-popup-action" href="${detailsUrl(marker)}">View details</a>
        </div>`;
}

export function updateEmptyMessage(visibleCount) {
    const note = elements.mapEmptyNote;
    if (!note) return;

    let text = "";
    if (!state.hasLoadedMarkers && state.isLoading) {
        text = "Loading map data...";
    } else if (visibleCount === 0) {
        text = state.allMarkers.length === 0
            ? "No needs or resources in this area."
            : "No markers match the selected filters.";
    }

    note.textContent = text;
    note.style.display = text ? "block" : "none";   // "" would fall back to the CSS display:none
}

export function updateCapMessage(truncated) {
    if (elements.mapCapNote) elements.mapCapNote.style.display = truncated ? "block" : "none";
}

// A marker is rebuilt only when something shown in it changed
function signatureOf(marker) {
    const origin = state.searchLocation ? `${state.searchLocation.lat},${state.searchLocation.lng}` : "";
    return [marker.title, marker.status, marker.priority, marker.when, marker.description,
            marker.contactInfo, marker.category, marker.region, origin].join("|");
}

export function renderMarkers(markers) {
    const wanted = new Map(markers.map(m => [markerKey(m.kind, m.id), m]));

    // Remove markers that are no longer wanted, or whose content changed
    for (const [key, entry] of state.markerIndex) {
        const marker = wanted.get(key);
        if (!marker || entry.signature !== signatureOf(marker)) {
            state.markerLayer.removeLayer(entry.leaflet);
            state.markerIndex.delete(key);
        }
    }

    // Add the missing ones. Markers that stay are left alone, so a reload does not close an open popup
    for (const [key, marker] of wanted) {
        if (state.markerIndex.has(key)) continue;

        const leaflet = L.marker([Number(marker.latitude), Number(marker.longitude)], { icon: createIcon(marker) })
            .bindPopup(buildPopupHtml(marker))
            .addTo(state.markerLayer);

        state.markerIndex.set(key, { leaflet, signature: signatureOf(marker) });
    }

    updateEmptyMessage(markers.length);
    openPendingFocus();
}

/** Opens the popup of the item chosen in the search box, as soon as its marker is on the map. */
export function openPendingFocus() {
    const focus = state.pendingFocus;
    if (!focus) return;

    const entry = state.markerIndex.get(markerKey(focus.kind, focus.id));
    if (!entry) return;   // not loaded yet, or hidden by a filter; reportUnfoundFocus explains after the load

    state.pendingFocus = null;

    // Expand the description section as soon as the popup is open
    entry.leaflet.once("popupopen", event => {
        event.popup.getElement()?.querySelector("details")?.setAttribute("open", "");
    });
    entry.leaflet.openPopup();
}

/** Called after a load has finished: tells the user why the chosen item is not shown. */
export function reportUnfoundFocus() {
    const focus = state.pendingFocus;
    if (!focus) return;
    state.pendingFocus = null;

    const key = markerKey(focus.kind, focus.id);
    const loaded = state.allMarkers.some(m => markerKey(m.kind, m.id) === key);

    const note = elements.mapEmptyNote;
    if (note) {
        note.textContent = loaded
            ? "That item is hidden by your filters. Reset the filters to see it."
            : "That item is not available on the map.";
        note.style.display = "block";
    }
}