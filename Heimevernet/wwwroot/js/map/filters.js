import { CONFIG } from "./config.js";
import { state, elements } from "./state.js";
import { getPriority, renderMarkers, updateEmptyMessage } from "./markers.js";
import { distanceFromPoint, formatLabel, isNeed, normalize } from "./utils.js";

function fillSelect(select, values, placeholder) {
    if (!select) return;

    const previous = select.value;
    const unique = [...new Set(values.filter(Boolean))]
        .sort((a, b) => String(a).localeCompare(String(b)));
    if (previous && !unique.includes(previous)) unique.push(previous); // an active filter stays selectable after panning

    select.replaceChildren(
        new Option(placeholder, ""),
        ...unique.map(v => new Option(formatLabel(v), String(v)))
    );
    select.value = previous;
}

/** Category and region options follow whatever is currently loaded. */
export function populateFilterOptions() {
    fillSelect(elements.mapFilterCategory, state.allMarkers.map(m => m.category), "All categories");
    fillSelect(elements.mapFilterRegion, state.allMarkers.map(m => m.region), "All regions");
}

const checkedValues = selector =>
    [...document.querySelectorAll(selector)].filter(i => i.checked).map(i => normalize(i.value));

function readFilters() {
    return {
        kinds: checkedValues(".map-filter-kind"),           // need, resource
        priorities: checkedValues(".map-filter-priority"),  // low, medium, critical
        category: elements.mapFilterCategory?.value ?? "",
        region: elements.mapFilterRegion?.value ?? "",
        needStatus: elements.mapFilterNeedStatus?.value ?? "",
        resourceStatus: elements.mapFilterResourceStatus?.value ?? "",
        radiusKm: Number(elements.mapFilterRadius?.value) || CONFIG.defaultRadiusKm
    };
}

function matches(marker, f) {
    if (!f.kinds.includes(normalize(marker.kind))) return false;
    if (f.category && marker.category !== f.category) return false;
    if (f.region && marker.region !== f.region) return false;

    if (isNeed(marker)) {
        if (!f.priorities.includes(normalize(getPriority(marker)))) return false;
        if (f.needStatus && marker.status !== f.needStatus) return false;
    } else if (f.resourceStatus && marker.status !== f.resourceStatus) {
        return false;
    }

    // A set location always filters by distance; the dropdown only picks the radius
    if (state.searchLocation) {
        const km = distanceFromPoint(marker, state.searchLocation);
        if (km === null || km > f.radiusKm) return false;
    }

    return true;
}

export function applyFilters() {
    if (!state.hasLoadedMarkers) {
        updateEmptyMessage(0);
        return;
    }

    const filters = readFilters();
    renderMarkers(state.allMarkers.filter(m => matches(m, filters)));
    updateDistanceNote(filters.radiusKm);
}

function updateDistanceNote(radiusKm) {
    const note = elements.mapFilterDistanceNote;
    if (!note) return;

    note.textContent = state.searchLocation
        ? `Showing markers within ${radiusKm} km of ${state.searchLocation.label}.`
        : "Search for an address or use your location to filter by distance.";
}

function resetFilters() {
    document.querySelectorAll(".map-filter-kind, .map-filter-priority").forEach(i => { i.checked = true; });

    [elements.mapFilterCategory, elements.mapFilterRegion,
     elements.mapFilterNeedStatus, elements.mapFilterResourceStatus].forEach(select => {
        if (select) select.value = "";
    });

    if (elements.mapFilterRadius) {
        elements.mapFilterRadius.value = String(CONFIG.defaultRadiusKm);
        elements.mapFilterRadius.disabled = true;
    }
    if (elements.globalSearchInput) elements.globalSearchInput.value = "";

    state.searchLayer.clearLayers();
    state.searchLocation = null;
    applyFilters();
}

function toggleFilterBox() {
    const box = elements.mapFilterBox;
    if (!box) return;

    box.classList.toggle("collapsed");   // the CSS hides only the body, so the button stays reachable
    elements.mapFilterToggle.setAttribute("aria-expanded", String(!box.classList.contains("collapsed")));
}

export function wireFilterControls() {
    document.querySelectorAll(".map-filter-kind, .map-filter-priority")
        .forEach(input => input.addEventListener("change", applyFilters));

    // The radius dropdown is wired in location.js, which owns the search circle
    [elements.mapFilterCategory, elements.mapFilterRegion,
     elements.mapFilterNeedStatus, elements.mapFilterResourceStatus]
        .forEach(el => el?.addEventListener("change", applyFilters));

    elements.mapFilterResetBtn?.addEventListener("click", resetFilters);
    elements.mapFilterToggle?.addEventListener("click", toggleFilterBox);
}