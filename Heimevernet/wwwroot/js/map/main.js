import { CONFIG } from "./config.js";
import { state, cacheElements, elements } from "./state.js";
import { wireFilterControls } from "./filters.js";
import { wireLocationControls } from "./location.js";
import { loadMarkers, scheduleLoad } from "./data.js";

const mapElement = document.getElementById("leafletMap");

function updateMeasurementMarkers() {
    if (!state.measurementLayer) {
        return;
    }

    state.measurementLayer.clearLayers();

    (state.mapActionPoints ?? []).forEach((point, index) => {
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
        panel.hidden = true;
        return;
    }

    panel.hidden = false;

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

if (!mapElement || typeof L === "undefined") {
    console.error("Map initialization failed: map element or Leaflet is missing.");
} else {
    cacheElements();

    state.map = L.map(mapElement, { zoomControl: false }).setView(CONFIG.initialCenter, CONFIG.initialZoom);
    L.tileLayer(CONFIG.tileUrl, {
        attribution: CONFIG.tileAttribution,
        maxZoom: CONFIG.maxZoom
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

    state.map.on("moveend", scheduleLoad);
    loadMarkers();
    updateMapActionDisplay();
}








































































































