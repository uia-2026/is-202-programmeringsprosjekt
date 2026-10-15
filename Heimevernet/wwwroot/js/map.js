(function () {
    'use strict';

    var MARKER_COLORS = {
        red: '#e4483c',
        yellow: '#f0a93b',
        green: '#2e9e5b',
        blue: '#3b7dd8',
        default: '#1d385a'
    };

    var MARKER_KIND = {
        NEED: 'Need',
        RESOURCE: 'Resource'
    };

    var DEFAULT_CENTER = [58.35, 7.6];
    var DEFAULT_ZOOM = 9;
    var detailsCounter = 0;

    var allMarkers = [];
    var markersLayer = null;
    var filterCenter = null;
    var searchMarker = null;

    var loadTimer = null;
    var requestSeq = 0;
    var hasLoadedMarkers = false;

    function getElement(id) {
        return document.getElementById(id);
    }

    function clamp(value, min, max) {
        return Math.max(min, Math.min(max, value));
    }

    function formatDate(isoString) {
        if (!isoString) return '';

        var date = new Date(isoString);

        if (isNaN(date.getTime())) return isoString;

        return date.toLocaleString('en-GB', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function getMarkerColor(marker) {
        if (marker.kind === MARKER_KIND.NEED) {
            return marker.isUrgent
                ? MARKER_COLORS.red
                : MARKER_COLORS.yellow;
        }

        if (
            marker.kind === MARKER_KIND.RESOURCE &&
            marker.status === 'Available'
        ) {
            return MARKER_COLORS.green;
        }

        return MARKER_COLORS.default;
    }

    window.toggleMapPopupDetails = function (detailsId) {
        var panel = getElement(detailsId);
        var button = getElement(detailsId + '-toggle');

        if (!panel || !button) return;

        var isHidden = panel.style.display === 'none';

        panel.style.display = isHidden ? 'block' : 'none';
        button.textContent = isHidden ? 'Hide details' : 'Show details';
    };

    function distanceKm(lat1, lon1, lat2, lon2) {
        var earthRadiusKm = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a =
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * Math.PI / 180) *
            Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);

        var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return earthRadiusKm * c;
    }

    function priorityBucket(priority) {
        switch (priority) {
            case 'Urgent':
            case 'High':
            case 'Critical':
                return 'urgent';

            case 'Planned':
            case 'Medium':
            case 'Low':
            default:
                return 'planned';
        }
    }

    function buildPopupHtml(marker) {
        var isNeed = marker.kind === MARKER_KIND.NEED;
        var whenLabel = isNeed ? 'Deadline' : 'Available from';

        var kindLabel = isNeed
            ? (marker.priority || 'Planned').toUpperCase() + ' NEED'
            : (marker.status || '').toUpperCase() + ' RESOURCE';

        var detailsUrl = isNeed
            ? '/Need/Details?id=' + encodeURIComponent(marker.id)
            : '/Resource/Details?id=' + encodeURIComponent(marker.id);

        var detailsId = 'map-popup-details-' + detailsCounter++;
        var markerColor = getMarkerColor(marker);
        var html = '';

        html += '<div class="map-popup-kind">' +
            escapeHtml(kindLabel) + '</div>';

        html += '<div class="map-popup-title">' +
            escapeHtml(marker.title) + '</div>';

        if (isNeed && marker.priority) {
            html += '<span class="map-popup-badge" style="background:' +
                markerColor + '">' +
                escapeHtml(marker.priority) + ' priority</span>';
        }

        if (filterCenter) {
            var distance = distanceKm(
                filterCenter.lat,
                filterCenter.lng,
                Number(marker.latitude),
                Number(marker.longitude)
            );

            html += '<div class="map-popup-row"><strong>Distance:</strong> ' +
                distance.toFixed(1) + ' km away</div>';
        }

        html += '<div class="map-popup-row"><strong>Type:</strong> ' +
            escapeHtml(marker.category) + '</div>';

        if (marker.region) {
            html += '<div class="map-popup-row"><strong>Region:</strong> ' +
                escapeHtml(marker.region) + '</div>';
        }

        if (marker.when) {
            html += '<div class="map-popup-row"><strong>' +
                whenLabel + ':</strong> ' +
                escapeHtml(formatDate(marker.when)) + '</div>';
        }

        html += '<div class="map-popup-row"><strong>Status:</strong> ' +
            escapeHtml(marker.status) + '</div>';

        html += '<a class="map-popup-action" href="' +
            detailsUrl + '">View details</a>';

        html += '<button type="button" id="' + detailsId +
            '-toggle" class="map-popup-toggle" ' +
            'onclick="toggleMapPopupDetails(\'' + detailsId +
            '\')">Show details</button>';

        html += '<div class="map-popup-extra" id="' + detailsId +
            '" style="display:none;">';

        if (marker.description) {
            html += '<div class="map-popup-row">' +
                escapeHtml(marker.description) + '</div>';
        }

        if (marker.contactInfo) {
            html += '<div class="map-popup-row"><strong>Contact:</strong> ' +
                escapeHtml(marker.contactInfo) + '</div>';
        }

        html += '</div>';

        return html;
    }

    function hasValidCoordinates(marker) {
        return Number.isFinite(Number(marker.latitude)) &&
            Number.isFinite(Number(marker.longitude)) &&
            Number(marker.latitude) >= -90 &&
            Number(marker.latitude) <= 90 &&
            Number(marker.longitude) >= -180 &&
            Number(marker.longitude) <= 180;
    }

    function renderMarkers(map, markers) {
        if (!markersLayer) {
            markersLayer = L.layerGroup().addTo(map);
        }

        markersLayer.clearLayers();

        var note = getElement('mapEmptyNote');

        if (markers.length === 0) {
            if (note) {
                note.textContent = !hasLoadedMarkers
                    ? 'Loading map data...'
                    : allMarkers.length === 0
                        ? 'No needs or resources in this area.'
                        : 'No needs or resources match your current filters.';

                note.style.display = 'block';
            }

            return;
        }

        if (note) note.style.display = 'none';

        markers.forEach(function (marker) {
            var circle = L.circleMarker(
                [Number(marker.latitude), Number(marker.longitude)],
                {
                    radius: 9,
                    color: '#ffffff',
                    weight: 2,
                    fillColor: getMarkerColor(marker),
                    fillOpacity: 0.95
                }
            );

            circle.bindPopup(buildPopupHtml(marker), {
                minWidth: 220
            });

            circle.addTo(markersLayer);
        });
    }

    function populateFilterOptions(markers) {
        var categorySelect = getElement('mapFilterCategory');
        var regionSelect = getElement('mapFilterRegion');
        var needStatusSelect = getElement('mapFilterNeedStatus');
        var resourceStatusSelect = getElement('mapFilterResourceStatus');

        function uniqueSorted(values) {
            var seen = Object.create(null);
            var result = [];

            values.forEach(function (value) {
                if (value && !seen[value]) {
                    seen[value] = true;
                    result.push(value);
                }
            });

            return result.sort();
        }

        function fillSelect(select, values) {
            if (!select) return;

            var current = select.value;

            if (current && values.indexOf(current) === -1) {
                values.push(current);
                values.sort();
            }

            while (select.options.length > 1) {
                select.remove(1);
            }

            values.forEach(function (value) {
                var option = document.createElement('option');
                option.value = value;
                option.textContent = value;
                select.appendChild(option);
            });

            select.value = current;
        }

        fillSelect(
            categorySelect,
            uniqueSorted(markers.map(function (marker) {
                return marker.category;
            }))
        );

        fillSelect(
            regionSelect,
            uniqueSorted(markers.map(function (marker) {
                return marker.region;
            }))
        );

        fillSelect(
            needStatusSelect,
            uniqueSorted(markers
                .filter(function (marker) {
                    return marker.kind === MARKER_KIND.NEED;
                })
                .map(function (marker) {
                    return marker.status;
                }))
        );

        fillSelect(
            resourceStatusSelect,
            uniqueSorted(markers
                .filter(function (marker) {
                    return marker.kind === MARKER_KIND.RESOURCE;
                })
                .map(function (marker) {
                    return marker.status;
                }))
        );
    }

    function getFilterState() {
        var kindBoxes = document.querySelectorAll('.map-filter-kind:checked');
        var priorityBoxes = document.querySelectorAll('.map-filter-priority:checked');

        return {
            kinds: Array.prototype.map.call(kindBoxes, function (checkbox) {
                return checkbox.value;
            }),
            priorities: Array.prototype.map.call(priorityBoxes, function (checkbox) {
                return checkbox.value;
            }),
            category: getElement('mapFilterCategory')?.value || '',
            region: getElement('mapFilterRegion')?.value || '',
            needStatus: getElement('mapFilterNeedStatus')?.value || '',
            resourceStatus: getElement('mapFilterResourceStatus')?.value || '',
            radius: parseFloat(getElement('mapFilterRadius')?.value || '0')
        };
    }

    function applyFilters(map) {
        var state = getFilterState();

        var filtered = allMarkers.filter(function (marker) {
            if (state.kinds.indexOf(marker.kind) === -1) {
                return false;
            }

            if (
                marker.kind === MARKER_KIND.NEED &&
                marker.priority &&
                state.priorities.indexOf(priorityBucket(marker.priority)) === -1
            ) {
                return false;
            }

            if (state.category && marker.category !== state.category) {
                return false;
            }

            if (state.region && marker.region !== state.region) {
                return false;
            }

            if (
                marker.kind === MARKER_KIND.NEED &&
                state.needStatus &&
                marker.status !== state.needStatus
            ) {
                return false;
            }

            if (
                marker.kind === MARKER_KIND.RESOURCE &&
                state.resourceStatus &&
                marker.status !== state.resourceStatus
            ) {
                return false;
            }

            if (state.radius && filterCenter) {
                var distance = distanceKm(
                    filterCenter.lat,
                    filterCenter.lng,
                    Number(marker.latitude),
                    Number(marker.longitude)
                );

                if (distance > state.radius) {
                    return false;
                }
            }

            return true;
        });

        renderMarkers(map, filtered);
    }

    function scheduleLoad(map) {
        clearTimeout(loadTimer);

        loadTimer = setTimeout(function () {
            loadMarkers(map);
        }, 250);
    }

    function loadMarkers(map) {
        var bounds = map.getBounds();

        var params = new URLSearchParams({
            south: clamp(bounds.getSouth(), -90, 90),
            north: clamp(bounds.getNorth(), -90, 90),
            west: clamp(bounds.getWest(), -180, 180),
            east: clamp(bounds.getEast(), -180, 180)
        });

        var sequence = ++requestSeq;

        fetch('/Map/Data?' + params.toString())
            .then(function (response) {
                if (!response.ok) {
                    throw new Error(
                        'Map data request failed: ' + response.status
                    );
                }

                return response.json();
            })
            .then(function (data) {
                if (sequence !== requestSeq) return;

                var markers = Array.isArray(data.markers)
                    ? data.markers
                    : [];

                allMarkers = markers.filter(hasValidCoordinates);
                hasLoadedMarkers = true;

                populateFilterOptions(allMarkers);
                applyFilters(map);

                var capNote = getElement('mapCapNote');

                if (capNote) {
                    capNote.style.display = data.truncated ? 'block' : 'none';
                }
            })
            .catch(function (error) {
                if (sequence !== requestSeq) return;

                console.error('Could not load map data', error);

                var note = getElement('mapEmptyNote');

                if (note) {
                    note.textContent =
                        'Could not load map data. Please reload the page.';
                    note.style.display = 'block';
                }
            });
    }

    function wireFilterControls(map) {
        var box = getElement('mapFilterBox');
        var toggleButton = getElement('mapFilterToggle');
        var locationButton = getElement('mapFilterLocationBtn');
        var radiusSelect = getElement('mapFilterRadius');
        var distanceNote = getElement('mapFilterDistanceNote');
        var resetButton = getElement('mapFilterResetBtn');

        if (toggleButton && box) {
            toggleButton.addEventListener('click', function () {
                box.classList.toggle('collapsed');

                toggleButton.setAttribute(
                    'aria-expanded',
                    String(!box.classList.contains('collapsed'))
                );
            });
        }

        document.querySelectorAll(
            '.map-filter-kind, .map-filter-priority, ' +
            '#mapFilterCategory, #mapFilterRegion, ' +
            '#mapFilterNeedStatus, #mapFilterResourceStatus, ' +
            '#mapFilterRadius'
        ).forEach(function (element) {
            element.addEventListener('change', function () {
                applyFilters(map);
            });
        });

        if (locationButton) {
            locationButton.addEventListener('click', function () {
                if (!navigator.geolocation) {
                    if (distanceNote) {
                        distanceNote.textContent =
                            "Your browser doesn't support location.";
                    }

                    return;
                }

                locationButton.textContent = 'Locating…';

                navigator.geolocation.getCurrentPosition(
                    function (position) {
                        if (searchMarker) {
                            map.removeLayer(searchMarker);
                            searchMarker = null;
                        }

                        filterCenter = {
                            lat: position.coords.latitude,
                            lng: position.coords.longitude
                        };

                        locationButton.textContent = 'Location set';
                        locationButton.classList.add('active');

                        if (radiusSelect) radiusSelect.disabled = false;

                        if (distanceNote) {
                            distanceNote.textContent =
                                'Centered on your current location.';
                        }

                        applyFilters(map);
                    },
                    function () {
                        locationButton.textContent = 'Use my location';

                        if (distanceNote) {
                            distanceNote.textContent =
                                "Couldn't get your location. Check your browser's permissions.";
                        }
                    },
                    {
                        enableHighAccuracy: false,
                        timeout: 10000,
                        maximumAge: 60000
                    }
                );
            });
        }

        if (resetButton) {
            resetButton.addEventListener('click', function () {
                document.querySelectorAll(
                    '.map-filter-kind, .map-filter-priority'
                ).forEach(function (checkbox) {
                    checkbox.checked = true;
                });

                [
                    'mapFilterCategory',
                    'mapFilterRegion',
                    'mapFilterNeedStatus',
                    'mapFilterResourceStatus'
                ].forEach(function (id) {
                    var element = getElement(id);
                    if (element) element.value = '';
                });

                if (radiusSelect) {
                    radiusSelect.value = '';
                    radiusSelect.disabled = true;
                }

                filterCenter = null;

                if (locationButton) {
                    locationButton.textContent = 'Use my location';
                    locationButton.classList.remove('active');
                }

                if (distanceNote) distanceNote.textContent = '';

                if (searchMarker) {
                    map.removeLayer(searchMarker);
                    searchMarker = null;
                }

                var searchInput = getElement('globalSearchInput');
                if (searchInput) searchInput.value = '';

                applyFilters(map);
            });
        }
    }

    function wireAddressSearch(map) {
        var input = getElement('globalSearchInput');

        if (!input) return;

        var wrapper = input.parentElement;

        if (wrapper && !wrapper.style.position) {
            wrapper.style.position = 'relative';
        }

        var suggestionsBox = document.createElement('div');
        suggestionsBox.className = 'app-search-suggestions';
        suggestionsBox.style.display = 'none';

        if (wrapper) wrapper.appendChild(suggestionsBox);

        var debounceTimer = null;
        var currentResults = [];
        var requestNumber = 0;

        function hideSuggestions() {
            suggestionsBox.style.display = 'none';
            suggestionsBox.replaceChildren();
        }

        function selectAddress(index) {
            var item = currentResults[index];

            if (!item) return;

            input.value = item.label;
            hideSuggestions();

            if (searchMarker) {
                map.removeLayer(searchMarker);
            }

            searchMarker = L.circleMarker(
                [item.lat, item.lon],
                {
                    radius: 8,
                    color: MARKER_COLORS.default,
                    weight: 3,
                    fillColor: '#ffffff',
                    fillOpacity: 1
                }
            ).addTo(map);

            searchMarker.bindPopup(
                'Search center: ' + escapeHtml(item.label)
            );

            filterCenter = {
                lat: item.lat,
                lng: item.lon
            };

            var radiusSelect = getElement('mapFilterRadius');
            var locationButton = getElement('mapFilterLocationBtn');
            var distanceNote = getElement('mapFilterDistanceNote');

            if (radiusSelect) {
                radiusSelect.disabled = false;
                radiusSelect.value = '10';
            }

            if (locationButton) {
                locationButton.classList.add('active');
                locationButton.textContent = 'Location set';
            }

            if (distanceNote) {
                distanceNote.textContent = 'Centered on: ' + item.label;
            }

            map.flyTo([item.lat, item.lon], 13);
            applyFilters(map);
        }

        function renderSuggestions(items) {
            currentResults = items;
            suggestionsBox.replaceChildren();

            if (items.length === 0) {
                var empty = document.createElement('div');
                empty.className = 'app-search-suggestion-empty';
                empty.textContent = 'No matching address found.';
                suggestionsBox.appendChild(empty);
            } else {
                items.forEach(function (item, index) {
                    var row = document.createElement('div');
                    row.className = 'app-search-suggestion-item';
                    row.textContent = item.label;

                    row.addEventListener('click', function () {
                        selectAddress(index);
                    });

                    suggestionsBox.appendChild(row);
                });
            }

            suggestionsBox.style.display = 'block';
        }

        function fetchResults(query) {
            var currentRequest = ++requestNumber;

            var url =
                'https://ws.geonorge.no/adresser/v1/sok?sok=' +
                encodeURIComponent(query) +
                '&treffPerSide=6&fuzzy=true';

            fetch(url)
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error(
                            'Address search failed: ' + response.status
                        );
                    }

                    return response.json();
                })
                .then(function (data) {
                    if (currentRequest !== requestNumber) return;

                    var addresses = (data && data.adresser) || [];

                    var items = addresses
                        .filter(function (address) {
                            return address.representasjonspunkt &&
                                Number.isFinite(Number(address.representasjonspunkt.lat)) &&
                                Number.isFinite(Number(address.representasjonspunkt.lon));
                        })
                        .map(function (address) {
                            var label = address.adressetekst || '';

                            if (address.postnummer || address.poststed) {
                                label += ', ' +
                                    (address.postnummer || '') + ' ' +
                                    (address.poststed || '');
                            }

                            return {
                                label: label.trim(),
                                lat: Number(address.representasjonspunkt.lat),
                                lon: Number(address.representasjonspunkt.lon)
                            };
                        });

                    renderSuggestions(items);
                })
                .catch(function (error) {
                    if (currentRequest !== requestNumber) return;

                    console.error('Address search failed', error);
                    renderSuggestions([]);
                });
        }

        input.addEventListener('input', function () {
            var query = input.value.trim();

            if (debounceTimer) clearTimeout(debounceTimer);

            requestNumber++;

            if (query.length < 3) {
                hideSuggestions();
                return;
            }

            debounceTimer = setTimeout(function () {
                fetchResults(query);
            }, 300);
        });

        input.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') hideSuggestions();
        });

        document.addEventListener('click', function (event) {
            if (!wrapper || !wrapper.contains(event.target)) {
                hideSuggestions();
            }
        });
    }

    function init() {
        var mapElement = getElement('leafletMap');

        if (!mapElement || typeof L === 'undefined') return;

        var map = L.map(mapElement, {
            zoomControl: true
        }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);

        L.tileLayer(
            'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            {
                maxZoom: 19,
                attribution: '&copy; OpenStreetMap contributors'
            }
        ).addTo(map);

        setTimeout(function () {
            map.invalidateSize();
        }, 0);

        window.addEventListener('resize', function () {
            map.invalidateSize();
        });

        wireFilterControls(map);
        wireAddressSearch(map);

        map.on('moveend', function () {
            scheduleLoad(map);
        });

        loadMarkers(map);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
