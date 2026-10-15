import { COLORS, CONFIG } from "./config.js";
import { state, elements } from "./state.js";
import { applyFilters } from "./filters.js";
import { initAutocomplete } from "./autocomplete.js";
import { escapeHtml } from "./utils.js";

const getRadiusKm = () => Number(elements.mapFilterRadius?.value) || CONFIG.defaultRadiusKm;

function setNote(text) {
    if (elements.mapFilterDistanceNote) elements.mapFilterDistanceNote.textContent = text;
}

function createUserIcon() {
    return L.divIcon({
        className: "map-marker-icon",
        html: '<div class="map-user-dot" role="img" aria-label="Your location"></div>',
        iconSize: [22, 22],
        iconAnchor: [11, 11]
    });
}

/** Draws the location marker and the radius circle, then filters the markers around it. */
function drawSearchArea(fit) {
    const { lat, lng, label, isUser } = state.searchLocation;
    const color = isUser ? COLORS.user : COLORS.search;
    state.searchLayer.clearLayers();

    const pin = isUser
        ? L.marker([lat, lng], { icon: createUserIcon(), zIndexOffset: 1000, keyboard: false })
        : L.circleMarker([lat, lng], { radius: 8, color: "#ffffff", weight: 2, fillColor: color, fillOpacity: 1 });
    pin.bindPopup(`<strong>${escapeHtml(isUser ? "You are here" : label)}</strong>`).addTo(state.searchLayer);

    const circle = L.circle([lat, lng], {
        radius: getRadiusKm() * 1000,
        color, weight: 2, fillColor: color, fillOpacity: 0.08
    }).addTo(state.searchLayer);

    // Fitting the whole circle also makes sure every marker inside it gets loaded
    if (fit) state.map.fitBounds(circle.getBounds(), { padding: [20, 20] });
    applyFilters();
}

/** @param {{ lat: number, lng: number, label: string, isUser?: boolean }} location */
function setSearchLocation(location) {
    state.searchLocation = location;
    if (elements.mapFilterRadius) elements.mapFilterRadius.disabled = false;
    drawSearchArea(true);
}

const GEO_ERRORS = {
    1: "Location permission was denied. Allow it for this site in your browser settings, then try again.",
    2: "Your position is unavailable. Check that location services are enabled for your browser in your system settings.",
    3: "Finding your location timed out. Try again."
};

function useMyLocation() {
    if (!window.isSecureContext) {
        setNote("Location only works over HTTPS (or on localhost).");
        return;
    }
    if (!navigator.geolocation) {
        setNote("Your browser does not support location.");
        return;
    }

    const button = elements.mapFilterLocationBtn;
    const originalText = button.textContent;
    button.disabled = true;
    button.textContent = "Locating…";
    setNote("Waiting for your browser's location permission…");

    const restore = () => {
        button.disabled = false;
        button.textContent = originalText;
    };

    // Calling this is what makes the browser show its "allow location" prompt
    navigator.geolocation.getCurrentPosition(
        ({ coords }) => {
            if (elements.globalSearchInput) elements.globalSearchInput.value = "";
            setSearchLocation({ lat: coords.latitude, lng: coords.longitude, label: "your location", isUser: true });
            restore();
        },
        error => {
            setNote(GEO_ERRORS[error.code] ?? "Could not get your location.");
            restore();
        },
        { timeout: 15000, maximumAge: 60000 }
    );
}

export function wireLocationControls() {
    elements.mapFilterLocationBtn?.addEventListener("click", useMyLocation);

    elements.mapFilterRadius?.addEventListener("change", () => {
        if (state.searchLocation) drawSearchArea(true);
    });

    if (elements.globalSearchInput) {
        initAutocomplete(elements.globalSearchInput, item =>
            setSearchLocation({ lat: item.lat, lng: item.lng, label: item.label }));
    }
}