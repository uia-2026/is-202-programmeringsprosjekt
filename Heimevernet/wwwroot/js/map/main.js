import { CONFIG, createBaseLayers } from "./config.js";
import { state, cacheElements, elements } from "./state.js";
import { wireFilterControls } from "./filters.js";
import { wireLocationControls } from "./location.js";
import { loadMarkers, scheduleLoad } from "./data.js";
import { readMapRequest } from "./focus.js";

const mapElement = document.getElementById("leafletMap");

function updateMeasurementMarkers() {
    if (!state.measurementLayer) {
        return;
    }

    state.measurementLayer.clearLayers();

    const points = state.mapActionPoints ?? [];

    points.forEach((point, index) => {
        L.circleMarker([point.lat, point.lng], {
            radius: 8,
            color: "#ffffff",
            weight: 2,
            fillColor: index === 0 ? "#1d5fa7" : "#d45b35",
            fillOpacity: 1,
            interactive: false
        }).bindTooltip(index === 0 ? "Start point" : "End point", {
            direction: "top",
            offset: [0, -8]
        }).addTo(state.measurementLayer);
    });

    if (points.length >= 2) {
        const first = points[0];
        const second = points[1];
        const distanceKm = L.latLng(first.lat, first.lng).distanceTo(L.latLng(second.lat, second.lng)) / 1000;

        const line = L.polyline([
            [first.lat, first.lng],
            [second.lat, second.lng]
        ], {
            color: "#1d5fa7",
            weight: 4,
            opacity: 0.9,
            className: "map-measurement-line"
        });

        line.bindTooltip(`${distanceKm.toFixed(2)} km`, {
            permanent: true,
            direction: "center",
            className: "map-measurement-tooltip",
            offset: [0, 0]
        });

        line.addTo(state.measurementLayer);
    }
}

function toggleMeasurementLayer(visible) {
    if (!state.measurementLayer || !state.map) {
        return;
    }

    if (visible) {
        if (!state.map.hasLayer(state.measurementLayer)) {
            state.measurementLayer.addTo(state.map);
        }
        updateMeasurementMarkers();
        return;
    }

    if (state.map.hasLayer(state.measurementLayer)) {
        state.measurementLayer.remove();
    }
}

function setActionPanelVisible(visible) {
    const panel = elements.mapActionPanel;
    const toggle = elements.mapActionToggle;

    if (!panel || !toggle) {
        return;
    }

    panel.hidden = !visible;
    toggle.setAttribute("aria-expanded", String(visible));
    toggle.style.display = visible ? "none" : "flex";

    toggleMeasurementLayer(visible);
}

function updateMapActionDisplay() {
    const panel = elements.mapActionPanel;
    const distanceEl = elements.mapActionDistance;
    const hintEl = elements.mapActionHint;
    const needLink = elements.mapActionNeedCreate;
    const resourceLink = elements.mapActionResourceCreate;

    if (!panel || !distanceEl || !hintEl) {
        return;
    }

    const points = state.mapActionPoints ?? [];

    if (points.length === 0) {
        updateMeasurementMarkers();
        distanceEl.textContent = "No distance yet";
        hintEl.textContent = "Click two different points on the map to measure distance.";
        if (panel && panel.hidden === false) {
            setActionPanelVisible(false);
        }
        return;
    }

    setActionPanelVisible(true);

    if (points.length === 1) {
        updateMeasurementMarkers();
        distanceEl.textContent = "1 point selected";
        hintEl.textContent = "Choose a second point to measure the distance.";

        const lat = points[0].lat.toFixed(6);
        const lng = points[0].lng.toFixed(6);

        if (needLink) needLink.href = `/Need/Create?lat=${lat}&lng=${lng}`;
        if (resourceLink) resourceLink.href = `/Resource/Create?lat=${lat}&lng=${lng}`;
        return;
    }

    const first = points[0];
    const second = points[1];
    updateMeasurementMarkers();
    const distanceKm = L.latLng(first.lat, first.lng).distanceTo(L.latLng(second.lat, second.lng)) / 1000;

    distanceEl.textContent = `${distanceKm.toFixed(2)} km`;
    hintEl.textContent = `Distance from ${first.lat.toFixed(5)}, ${first.lng.toFixed(5)} to ${second.lat.toFixed(5)}, ${second.lng.toFixed(5)}.`;

    const lat = second.lat.toFixed(6);
    const lng = second.lng.toFixed(6);
    if (needLink) needLink.href = `/Need/Create?lat=${lat}&lng=${lng}`;
    if (resourceLink) resourceLink.href = `/Resource/Create?lat=${lat}&lng=${lng}`;
}

function toggleActionPanel() {
    const panel = elements.mapActionPanel;
    const toggle = elements.mapActionToggle;
    if (!panel || !toggle) {
        return;
    }

    const shouldShow = panel.hidden;
    setActionPanelVisible(shouldShow);
}

if (!mapElement || typeof L === "undefined") {
    console.error("Map initialization failed: map element or Leaflet is missing.");
} else {
    cacheElements();

    state.map = L.map(mapElement, { zoomControl: false }).setView(CONFIG.initialCenter, CONFIG.initialZoom);

    const baseLayers = createBaseLayers();
    const defaultBaseLayerName = CONFIG.defaultBaseLayerName ?? Object.keys(baseLayers)[0];
    const defaultBaseLayer = baseLayers[defaultBaseLayerName] ?? Object.values(baseLayers)[0];

    if (defaultBaseLayer) {
        defaultBaseLayer.addTo(state.map);
    }

    const validBaseLayers = Object.fromEntries(
        Object.entries(baseLayers).filter(([, layer]) => !!layer)
    );

    L.control.layers(validBaseLayers, {}, {
        position: "bottomright",
        collapsed: true
    }).addTo(state.map);

    L.control.zoom({ position: "bottomright" }).addTo(state.map);

    state.markerLayer = L.layerGroup().addTo(state.map);
    state.searchLayer = L.layerGroup().addTo(state.map);
    state.measurementLayer = L.layerGroup().addTo(state.map);
    state.mapActionPoints = [];

    state.map.on("click", function (event) {
        const selected = { lat: event.latlng.lat, lng: event.latlng.lng };
        const points = state.mapActionPoints ?? [];

        if (points.length === 0) {
            state.mapActionPoints = [selected];
            updateMapActionDisplay();
            return;
        }

        if (points.length === 1) {
            const first = points[0];
            const samePointDistance = L.latLng(first.lat, first.lng).distanceTo(L.latLng(selected.lat, selected.lng));

            if (samePointDistance < 5) {
                elements.mapActionHint.textContent = "Choose a different point from the first one.";
                return;
            }

            state.mapActionPoints = [first, selected];
            updateMapActionDisplay();
            return;
        }

        state.mapActionPoints = [selected];
        updateMapActionDisplay();
    });

    wireFilterControls();
    wireLocationControls();

    elements.mapActionToggle?.addEventListener("click", toggleActionPanel);
    elements.mapActionClose?.addEventListener("click", () => setActionPanelVisible(false));

    state.map.on("moveend", scheduleLoad);
    loadMarkers();
    updateMapActionDisplay();
}

const request = readMapRequest(new URLSearchParams(window.location.search));
if (request) {
    state.pendingFocus = request.focus;
    state.map.setView([request.lat, request.lng],
        request.focus ? CONFIG.focusZoom : CONFIG.addressZoom, { animate: false });
    history.replaceState(null, "", window.location.pathname);
}
