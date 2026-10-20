import { CONFIG } from "./config.js";
import { state, cacheElements } from "./state.js";
import { wireFilterControls } from "./filters.js";
import { wireLocationControls } from "./location.js";
import { loadMarkers, scheduleLoad } from "./data.js";

const mapElement = document.getElementById("leafletMap");

if (!mapElement || typeof L === "undefined") {
    console.error("Map initialization failed: map element or Leaflet is missing.");
} else {
    cacheElements();

    state.map = L.map(mapElement).setView(CONFIG.initialCenter, CONFIG.initialZoom);
    L.tileLayer(CONFIG.tileUrl, {
        attribution: CONFIG.tileAttribution,
        maxZoom: CONFIG.maxZoom
    }).addTo(state.map);

    state.markerLayer = L.layerGroup().addTo(state.map);
    state.searchLayer = L.layerGroup().addTo(state.map);

    wireFilterControls();
    wireLocationControls();

    state.map.on("moveend", scheduleLoad);
    loadMarkers();
}