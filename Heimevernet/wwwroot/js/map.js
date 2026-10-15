
(() => {
    "use strict";

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    const CONFIG = {
        dataUrl: "/Map/Data",
        initialCenter: [58.35, 7.6],
        initialZoom: 9,
        maxZoom: 19,
        requestDebounceMs: 250,
        searchRadiusKm: 10,
        maxDisplayedMarkersPerKind: 500,
        tileUrl: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        tileAttribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    };

    const MARKER_KIND = {
        NEED: "Need",
        RESOURCE: "Resource"
    };

    const PRIORITIES = ["Low", "Medium", "Critical"];

    const COLORS = {
        Low: "#2563eb",
        Medium: "#ca8a04",
        Critical: "#dc2626",
        ResourceAvailable: "#16a34a",
        ResourceBusy: "#64748b",
        Search: "#7c3aed"
    };

    const NEED_STATUSES = ["New", "UnderReview", "Assigned", "Resolved"];
    const RESOURCE_STATUSES = ["Available", "Busy"];

    // ------------------------------------------------------------
    // State
    // ------------------------------------------------------------

    let map;
    let markerLayer;
    let searchLayer;

    let allMarkers = [];
    let renderedMarkers = [];

    let hasLoadedMarkers = false;
    let isLoading = false;
    let requestSequence = 0;
    let debounceTimeout = null;

    let searchLocation = null;
    let searchRadiusCircle = null;

    const elements = {};

    // ------------------------------------------------------------
    // General utilities
    // ------------------------------------------------------------

    function getElement(id) {
        return document.getElementById(id);
    }

    function cacheElements() {
        [
            "mapFilterBox",
            "mapFilterToggle",
            "mapFilterCategory",
            "mapFilterRegion",
            "mapFilterNeedStatus",
            "mapFilterResourceStatus",
            "mapFilterRadius",
            "mapFilterLocationBtn",
            "mapFilterDistanceNote",
            "mapFilterResetBtn",
            "mapEmptyNote",
            "mapCapNote",
            "globalSearchInput"
        ].forEach(id => {
            elements[id] = getElement(id);
        });
    }

    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, character => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#39;"
        })[character]);
    }

    function normalize(value) {
        return String(value ?? "").trim().toLowerCase();
    }

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function formatDate(value) {
        if (!value) {
            return "Not specified";
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return "Not specified";
        }

        return new Intl.DateTimeFormat("en-GB", {
            dateStyle: "medium",
            timeStyle: "short"
        }).format(date);
    }

    function formatLabel(value) {
        if (!value) {
            return "Not specified";
        }

        return String(value)
            .replace(/([a-z])([A-Z])/g, "$1 $2")
            .replace(/_/g, " ");
    }

    function isValidCoordinate(value, min, max) {
        return value !== null &&
            value !== undefined &&
            value !== "" &&
            Number.isFinite(Number(value)) &&
            Number(value) >= min &&
            Number(value) <= max;
    }

    function hasValidCoordinates(marker) {
        return marker &&
            isValidCoordinate(marker.latitude, -90, 90) &&
            isValidCoordinate(marker.longitude, -180, 180);
    }

    function isNeed(marker) {
        return marker.kind === MARKER_KIND.NEED;
    }

    function isResource(marker) {
        return marker.kind === MARKER_KIND.RESOURCE;
    }

    function getPriority(marker) {
        const priority = String(marker.priority ?? "");

        return PRIORITIES.includes(priority) ? priority : "Medium";
    }

    function getResourceColor(marker) {
        return marker.status === "Available"
            ? COLORS.ResourceAvailable
            : COLORS.ResourceBusy;
    }

    function getMarkerColor(marker) {
        if (isResource(marker)) {
            return getResourceColor(marker);
        }

        return COLORS[getPriority(marker)];
    }

    function getPriorityLabel(marker) {
        return isNeed(marker)
            ? formatLabel(getPriority(marker))
            : null;
    }

    // ------------------------------------------------------------
    // Distance calculations
    // ------------------------------------------------------------

    function distanceKm(lat1, lon1, lat2, lon2) {
        const earthRadiusKm = 6371;
        const toRadians = degrees => degrees * Math.PI / 180;

        const latitudeDifference = toRadians(lat2 - lat1);
        const longitudeDifference = toRadians(lon2 - lon1);

        const a =
            Math.sin(latitudeDifference / 2) ** 2 +
            Math.cos(toRadians(lat1)) *
            Math.cos(toRadians(lat2)) *
            Math.sin(longitudeDifference / 2) ** 2;

        return earthRadiusKm * 2 * Math.atan2(
            Math.sqrt(a),
            Math.sqrt(1 - a)
        );
    }

    function getDistanceFromSearch(marker) {
        if (!searchLocation || !hasValidCoordinates(marker)) {
            return null;
        }

        return distanceKm(
            searchLocation.lat,
            searchLocation.lng,
            Number(marker.latitude),
            Number(marker.longitude)
        );
    }

    // ------------------------------------------------------------
    // Marker icons
    //
    // Needs: coloured diamonds, with a priority label.
    // Resources: squares, with availability indicated by colour.
    // Shapes remain distinct even without relying on colour alone.
    // ------------------------------------------------------------

    function createMarkerIcon(marker) {
        const color = getMarkerColor(marker);
        const resource = isResource(marker);

        const shapeClass = resource
            ? "map-marker-shape map-marker-resource"
            : "map-marker-shape map-marker-need";

        const symbol = resource
            ? "+"
            : getPriority(marker) === "Critical"
                ? "!"
                : getPriority(marker) === "Medium"
                    ? "M"
                    : "L";

        const html = `
            <div
                class="${shapeClass}"
                style="--marker-color:${color}"
                title="${escapeHtml(
                    resource
                        ? `Resource: ${marker.status}`
                        : `Need priority: ${getPriority(marker)}`
                )}"
                aria-label="${escapeHtml(
                    resource
                        ? `Resource, ${marker.status}`
                        : `Need, ${getPriority(marker)} priority`
                )}">
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

    function installMarkerStyles() {
        if (getElement("mapMarkerStyles")) {
            return;
        }

        const style = document.createElement("style");
        style.id = "mapMarkerStyles";

        style.textContent = `
            .map-marker-icon {
                background: transparent;
                border: none;
            }

            .map-marker-shape {
                width: 25px;
                height: 25px;
                display: flex;
                align-items: center;
                justify-content: center;
                background: var(--marker-color);
                color: #fff;
                border: 2px solid #fff;
                box-shadow: 0 1px 5px rgba(0, 0, 0, .45);
                font-size: 12px;
                font-weight: 800;
                box-sizing: border-box;
            }

            .map-marker-need {
                transform: rotate(45deg);
                border-radius: 3px;
            }

            .map-marker-need span {
                transform: rotate(-45deg);
            }

            .map-marker-resource {
                border-radius: 5px;
                font-size: 20px;
            }

            .map-popup {
                min-width: 190px;
                max-width: 280px;
            }

            .map-popup h3 {
                font-size: 1rem;
                margin: 0 0 8px;
                overflow-wrap: anywhere;
            }

            .map-popup p {
                margin: 4px 0;
                overflow-wrap: anywhere;
            }

            .map-popup-details {
                margin-top: 8px;
            }

            .map-popup-details summary {
                cursor: pointer;
                font-weight: 600;
            }

            .map-popup-badge {
                display: inline-block;
                padding: 3px 7px;
                border-radius: 4px;
                color: white;
                font-size: .75rem;
                font-weight: 700;
            }
        `;

        document.head.appendChild(style);
    }

    // ------------------------------------------------------------
    // Popup content
    // ------------------------------------------------------------

    function buildPopupHtml(marker) {
        const resource = isResource(marker);
        const typeLabel = resource ? "Resource" : "Need";
        const color = getMarkerColor(marker);

        const priorityHtml = isNeed(marker)
            ? `<p><strong>Priority:</strong> ${escapeHtml(getPriorityLabel(marker))}</p>`
            : "";

        const availabilityHtml = resource
            ? `<p><strong>Availability:</strong> ${escapeHtml(formatLabel(marker.status))}</p>`
            : "";

        const relevantDateHtml = marker.when
            ? `<p><strong>${resource ? "Available from" : "Relevant date"}:</strong>
                   ${escapeHtml(formatDate(marker.when))}</p>`
            : "";

        const distance = getDistanceFromSearch(marker);
        const distanceHtml = distance !== null
            ? `<p><strong>Distance:</strong> ${distance.toFixed(1)} km</p>`
            : "";

        const descriptionHtml = marker.description
            ? `<p>${escapeHtml(marker.description)}</p>`
            : "";

        const contactHtml = marker.contactInfo
            ? `<p><strong>Contact:</strong> ${escapeHtml(marker.contactInfo)}</p>`
            : "";

        return `
            <div class="map-popup">
                <h3>${escapeHtml(marker.title || typeLabel)}</h3>

                <p>
                    <span class="map-popup-badge"
                          style="background:${color}">
                        ${escapeHtml(typeLabel)}
                    </span>
                </p>

                <p><strong>Category:</strong> ${escapeHtml(marker.category)}</p>
                <p><strong>Region:</strong> ${escapeHtml(marker.region)}</p>
                ${priorityHtml}
                ${availabilityHtml}
                ${relevantDateHtml}
                ${distanceHtml}

                <details class="map-popup-details">
                    <summary>More details</summary>
                    ${descriptionHtml}
                    ${contactHtml}
                    <p><strong>Status:</strong> ${escapeHtml(formatLabel(marker.status))}</p>
                </details>
            </div>`;
    }

    // ------------------------------------------------------------
    // Marker rendering
    // ------------------------------------------------------------

    function renderMarkers(markers) {
        markerLayer.clearLayers();
        renderedMarkers = [];

        markers.forEach(marker => {
            const leafletMarker = L.marker(
                [Number(marker.latitude), Number(marker.longitude)],
                {
                    icon: createMarkerIcon(marker),
                    title: marker.title || marker.kind
                }
            );

            leafletMarker.bindPopup(buildPopupHtml(marker));
            leafletMarker.addTo(markerLayer);

            renderedMarkers.push({
                data: marker,
                leafletMarker
            });
        });

        updateEmptyMessage(markers.length);
    }

    function updateEmptyMessage(visibleCount) {
        const emptyNote = elements.mapEmptyNote;

        if (!emptyNote) {
            return;
        }

        if (!hasLoadedMarkers && isLoading) {
            emptyNote.textContent = "Loading map data...";
            emptyNote.style.display = "";
            return;
        }

        if (visibleCount === 0) {
            emptyNote.textContent = allMarkers.length === 0
                ? "No needs or resources in this area."
                : "No markers match the selected filters.";

            emptyNote.style.display = "";
            return;
        }

        emptyNote.style.display = "none";
    }

    function updateCapMessage(truncated) {
        const capNote = elements.mapCapNote;

        if (!capNote) {
            return;
        }

        capNote.style.display = truncated ? "" : "none";
    }

    // ------------------------------------------------------------
    // Filter option population
    // ------------------------------------------------------------

    function fillSelect(select, values, placeholder) {
        if (!select) {
            return;
        }

        const previousValue = select.value;
        const uniqueValues = [...new Set(
            values.filter(value => value !== null && value !== undefined && value !== "")
        )].sort((a, b) => String(a).localeCompare(String(b)));

        select.replaceChildren();

        const defaultOption = document.createElement("option");
        defaultOption.value = "";
        defaultOption.textContent = placeholder;
        select.appendChild(defaultOption);

        uniqueValues.forEach(value => {
            const option = document.createElement("option");
            option.value = String(value);
            option.textContent = formatLabel(value);
            select.appendChild(option);
        });

        // Keep the selected filter when panning to a different area.
        if (previousValue && !uniqueValues.some(value => String(value) === previousValue)) {
            const preservedOption = document.createElement("option");
            preservedOption.value = previousValue;
            preservedOption.textContent = formatLabel(previousValue);
            select.appendChild(preservedOption);
        }

        select.value = previousValue;
    }

    function populateFilterOptions() {
        fillSelect(
            elements.mapFilterCategory,
            allMarkers.map(marker => marker.category),
            "All categories"
        );

        fillSelect(
            elements.mapFilterRegion,
            allMarkers.map(marker => marker.region),
            "All regions"
        );

        fillSelect(
            elements.mapFilterNeedStatus,
            NEED_STATUSES,
            "All need statuses"
        );

        fillSelect(
            elements.mapFilterResourceStatus,
            RESOURCE_STATUSES,
            "All resource statuses"
        );
    }

    // ------------------------------------------------------------
    // Read filter state
    // ------------------------------------------------------------

    function getCheckedValues(selector) {
        return [...document.querySelectorAll(selector)]
            .filter(input => input.checked)
            .map(input => input.value);
    }

    function getFilterState() {
        const kinds = getCheckedValues(".map-filter-kind");
        const priorities = getCheckedValues(".map-filter-priority");

        return {
            kinds,
            priorities,
            category: elements.mapFilterCategory?.value ?? "",
            region: elements.mapFilterRegion?.value ?? "",
            needStatus: elements.mapFilterNeedStatus?.value ?? "",
            resourceStatus: elements.mapFilterResourceStatus?.value ?? "",
            radiusEnabled: elements.mapFilterRadius?.checked ?? false
        };
    }

    function matchesFilters(marker, filters) {
        if (!filters.kinds.includes(marker.kind)) {
            return false;
        }

        if (filters.category && marker.category !== filters.category) {
            return false;
        }

        if (filters.region && marker.region !== filters.region) {
            return false;
        }

        if (isNeed(marker)) {
            if (filters.priorities.length > 0 &&
                !filters.priorities.includes(getPriority(marker))) {
                return false;
            }

            if (filters.needStatus && marker.status !== filters.needStatus) {
                return false;
            }
        }

        if (isResource(marker) &&
            filters.resourceStatus &&
            marker.status !== filters.resourceStatus) {
            return false;
        }

        if (filters.radiusEnabled && searchLocation) {
            const distance = getDistanceFromSearch(marker);

            if (distance === null || distance > CONFIG.searchRadiusKm) {
                return false;
            }
        }

        return true;
    }

    function applyFilters() {
        if (!hasLoadedMarkers) {
            updateEmptyMessage(0);
            return;
        }

        const filters = getFilterState();
        const filteredMarkers = allMarkers.filter(marker =>
            matchesFilters(marker, filters)
        );

        renderMarkers(filteredMarkers);
        updateDistanceNote(filters.radiusEnabled);
    }

    function updateDistanceNote(radiusEnabled) {
        const note = elements.mapFilterDistanceNote;

        if (!note) {
            return;
        }

        if (!searchLocation) {
            note.textContent = "Search for an address to filter by distance.";
            return;
        }

        if (radiusEnabled) {
            note.textContent =
                `Showing markers within ${CONFIG.searchRadiusKm} km of the searched location.`;
        } else {
            note.textContent =
                "Address found. Enable the distance filter to limit results.";
        }
    }

    // ------------------------------------------------------------
    // Fetch markers for the currently visible map bounds
    // ------------------------------------------------------------

    function scheduleLoad() {
        window.clearTimeout(debounceTimeout);

        debounceTimeout = window.setTimeout(() => {
            loadMarkers();
        }, CONFIG.requestDebounceMs);
    }

    async function loadMarkers() {
        if (!map) {
            return;
        }

        const bounds = map.getBounds();
        const southWest = bounds.getSouthWest();
        const northEast = bounds.getNorthEast();

        const params = new URLSearchParams({
            south: String(southWest.lat),
            north: String(northEast.lat),
            west: String(southWest.lng),
            east: String(northEast.lng)
        });

        const currentRequest = ++requestSequence;
        isLoading = true;

        if (!hasLoadedMarkers) {
            updateEmptyMessage(0);
        }

        try {
            const response = await fetch(
                `${CONFIG.dataUrl}?${params.toString()}`,
                {
                    method: "GET",
                    headers: {
                        Accept: "application/json"
                    },
                    credentials: "same-origin"
                }
            );

            if (!response.ok) {
                throw new Error(`Map request failed: ${response.status}`);
            }

            const data = await response.json();

            // Ignore responses from older requests after a pan or zoom.
            if (currentRequest !== requestSequence) {
                return;
            }

            if (!Array.isArray(data.markers)) {
                throw new Error("The map API returned an invalid marker list.");
            }

            allMarkers = data.markers.filter(hasValidCoordinates);
            hasLoadedMarkers = true;

            updateCapMessage(Boolean(data.truncated));
            populateFilterOptions();
            applyFilters();
        } catch (error) {
            if (currentRequest === requestSequence) {
                console.error("Unable to load map markers:", error);

                if (!hasLoadedMarkers && elements.mapEmptyNote) {
                    elements.mapEmptyNote.textContent =
                        "Unable to load map data. Try refreshing the page.";
                    elements.mapEmptyNote.style.display = "";
                }
            }
        } finally {
            if (currentRequest === requestSequence) {
                isLoading = false;
            }
        }
    }

    // ------------------------------------------------------------
    // Address search
    // ------------------------------------------------------------

    function setSearchLocation(latitude, longitude, label) {
        searchLocation = {
            lat: Number(latitude),
            lng: Number(longitude),
            label: label || ""
        };

        if (searchLayer) {
            searchLayer.clearLayers();
        }

        if (searchRadiusCircle) {
            searchLayer.removeLayer(searchRadiusCircle);
        }

        const searchMarker = L.circleMarker(
            [searchLocation.lat, searchLocation.lng],
            {
                radius: 8,
                color: "#ffffff",
                weight: 2,
                fillColor: COLORS.Search,
                fillOpacity: 1
            }
        ).bindPopup(`<strong>${escapeHtml(label || "Searched location")}</strong>`);

        searchMarker.addTo(searchLayer);

        searchRadiusCircle = L.circle(
            [searchLocation.lat, searchLocation.lng],
            {
                radius: CONFIG.searchRadiusKm * 1000,
                color: COLORS.Search,
                weight: 2,
                fillColor: COLORS.Search,
                fillOpacity: 0.08
            }
        );

        searchRadiusCircle.addTo(searchLayer);

        const radiusEnabled = elements.mapFilterRadius;

        if (radiusEnabled) {
            radiusEnabled.checked = true;
        }

        map.setView(
            [searchLocation.lat, searchLocation.lng],
            Math.max(map.getZoom(), 12)
        );

        applyFilters();
    }

    async function searchAddress() {
        const input = elements.globalSearchInput;
        const query = input?.value?.trim();

        if (!query) {
            return;
        }

        const button = elements.mapFilterLocationBtn;

        if (button) {
            button.disabled = true;
        }

        try {
            const params = new URLSearchParams({
                sok: query,
                treffPerSide: "1",
                fuzzy: "true"
            });

            const response = await fetch(
                `https://ws.geonorge.no/adresser/v1/sok?${params.toString()}`,
                {
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`Address search failed: ${response.status}`);
            }

            const data = await response.json();
            const address = data.adresser?.[0];

            if (!address?.代表spunkt) {
                // Geonorge normally returns representasjonspunkt for an address.
                // Handle missing coordinates below as a failed search.
                if (!address?.representasjonspunkt) {
                    throw new Error("No matching address was found.");
                }
            }

            const point = address.representasjonspunkt;

            if (!point ||
                !Number.isFinite(Number(point.lat)) ||
                !Number.isFinite(Number(point.lon))) {
                throw new Error("The address has no valid coordinates.");
            }

            const label = [
                address.adressetekst,
                address.postnummer,
                address.poststed
            ].filter(Boolean).join(", ");

            setSearchLocation(point.lat, point.lon, label || query);
        } catch (error) {
            console.error("Address search failed:", error);

            if (elements.mapFilterDistanceNote) {
                elements.mapFilterDistanceNote.textContent =
                    "Address not found. Try a more specific Norwegian address.";
            }
        } finally {
            if (button) {
                button.disabled = false;
            }
        }
    }

    function wireAddressSearch() {
        elements.mapFilterLocationBtn?.addEventListener("click", searchAddress);

        elements.globalSearchInput?.addEventListener("keydown", event => {
            if (event.key === "Enter") {
                event.preventDefault();
                searchAddress();
            }
        });
    }

    // ------------------------------------------------------------
    // Filter controls
    // ------------------------------------------------------------

    function wireFilterControls() {
        document.querySelectorAll(
            ".map-filter-kind, .map-filter-priority"
        ).forEach(input => {
            input.addEventListener("change", applyFilters);
        });

        [
            elements.mapFilterCategory,
            elements.mapFilterRegion,
            elements.mapFilterNeedStatus,
            elements.mapFilterResourceStatus,
            elements.mapFilterRadius
        ].forEach(element => {
            element?.addEventListener("change", applyFilters);
        });

        elements.mapFilterResetBtn?.addEventListener("click", () => {
            document.querySelectorAll(
                ".map-filter-kind, .map-filter-priority"
            ).forEach(input => {
                input.checked = true;
            });

            if (elements.mapFilterCategory) {
                elements.mapFilterCategory.value = "";
            }

            if (elements.mapFilterRegion) {
                elements.mapFilterRegion.value = "";
            }

            if (elements.mapFilterNeedStatus) {
                elements.mapFilterNeedStatus.value = "";
            }

            if (elements.mapFilterResourceStatus) {
                elements.mapFilterResourceStatus.value = "";
            }

            if (elements.mapFilterRadius) {
                elements.mapFilterRadius.checked = false;
            }

            if (searchLayer) {
                searchLayer.clearLayers();
            }

            searchLocation = null;
            searchRadiusCircle = null;

            updateDistanceNote(false);
            applyFilters();
        });

        elements.mapFilterToggle?.addEventListener("click", () => {
            const filterBox = elements.mapFilterBox;

            if (!filterBox) {
                return;
            }

            const isHidden = filterBox.style.display === "none";
            filterBox.style.display = isHidden ? "" : "none";
            elements.mapFilterToggle.setAttribute(
                "aria-expanded",
                String(isHidden)
            );
        });
    }

    // ------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------

    function init() {
        const mapElement = getElement("leafletMap");

        if (!mapElement || typeof L === "undefined") {
            console.error("Map initialization failed: map element or Leaflet is missing.");
            return;
        }

        cacheElements();
        installMarkerStyles();

        map = L.map(mapElement).setView(
            CONFIG.initialCenter,
            CONFIG.initialZoom
        );

        L.tileLayer(CONFIG.tileUrl, {
            attribution: CONFIG.tileAttribution,
            maxZoom: CONFIG.maxZoom
        }).addTo(map);

        markerLayer = L.layerGroup().addTo(map);
        searchLayer = L.layerGroup().addTo(map);

        wireFilterControls();
        wireAddressSearch();

        map.on("moveend", scheduleLoad);

        loadMarkers();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
