import { COLORS, PRIORITIES } from "./config.js";
import { state, elements } from "./state.js";
import { distanceFromPoint, escapeHtml, formatDate, formatLabel, isNeed, isResource, normalize } from "./utils.js";

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

export function renderMarkers(markers) {
    state.markerLayer.clearLayers();

    markers.forEach(marker => {
        L.marker([Number(marker.latitude), Number(marker.longitude)], { icon: createIcon(marker) })
            .bindPopup(buildPopupHtml(marker))
            .addTo(state.markerLayer);
    });

    updateEmptyMessage(markers.length);
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