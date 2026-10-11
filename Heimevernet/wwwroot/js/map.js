(function () {
    var MARKER_COLORS = {
        red: '#e4483c',
        yellow: '#f0a93b',
        green: '#2e9e5b',
        blue: '#3b7dd8'
    };

    // Centered on Agder / Kristiansand, where the seeded dev data lives.
    var DEFAULT_CENTER = [58.35, 7.6];
    var DEFAULT_ZOOM = 9;
    var detailsCounter = 0;

    function formatDate(isoString) {
        if (!isoString) return '';
        var d = new Date(isoString);
        if (isNaN(d.getTime())) return isoString;
        return d.toLocaleString('en-GB', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    window.toggleMapPopupDetails = function (detailsId) {
        var panel = document.getElementById(detailsId);
        var button = document.getElementById(detailsId + '-toggle');
        if (!panel || !button) return;

        var isHidden = panel.style.display === 'none';
        panel.style.display = isHidden ? 'block' : 'none';
        button.textContent = isHidden ? 'Hide details' : 'Show details';
    };

    function buildPopupHtml(marker) {
        var whenLabel = marker.kind === 'need' ? 'Deadline' : 'Available from';
        var kindLabel = marker.kind === 'need'
            ? (marker.priority || '').toUpperCase() + ' NEED'
            : (marker.status || '').toUpperCase() + ' RESOURCE';
        var detailsUrl = marker.kind === 'need'
            ? '/Need/Details?id=' + marker.id
            : '/Resource/Details?id=' + marker.id;
        var detailsId = 'map-popup-details-' + (detailsCounter++);

        var html = '';
        html += '<div class="map-popup-kind">' + escapeHtml(kindLabel) + '</div>';
        html += '<div class="map-popup-title">' + escapeHtml(marker.title) + '</div>';

        if (marker.priority) {
            html += '<span class="map-popup-badge" style="background:' + (MARKER_COLORS[marker.markerColor] || '#1d385a') + '">' +
                escapeHtml(marker.priority) + ' priority</span>';
        }

        html += '<div class="map-popup-row"><strong>Type:</strong> ' + escapeHtml(marker.category) + '</div>';
        if (marker.region) {
            html += '<div class="map-popup-row"><strong>Address:</strong> ' + escapeHtml(marker.region) + '</div>';
        }
        if (marker.when) {
            html += '<div class="map-popup-row"><strong>' + whenLabel + ':</strong> ' + formatDate(marker.when) + '</div>';
        }
        html += '<div class="map-popup-row"><strong>Status:</strong> ' + escapeHtml(marker.status) + '</div>';

        html += '<a class="map-popup-action" href="' + detailsUrl + '">View details</a>';

        html += '<button type="button" id="' + detailsId + '-toggle" class="map-popup-toggle" ' +
            'onclick="toggleMapPopupDetails(\'' + detailsId + '\')">Show details</button>';

        html += '<div class="map-popup-extra" id="' + detailsId + '" style="display:none;">';
        if (marker.description) {
            html += '<div class="map-popup-row">' + escapeHtml(marker.description) + '</div>';
        }
        if (marker.contactInfo) {
            html += '<div class="map-popup-row"><strong>Contact:</strong> ' + escapeHtml(marker.contactInfo) + '</div>';
        }
        html += '</div>';

        return html;
    }

    function plotMarkers(map, markers) {
        if (markers.length === 0) {
            var note = document.getElementById('mapEmptyNote');
            if (note) note.style.display = 'block';
            return;
        }

        var bounds = [];

        markers.forEach(function (marker) {
            var color = MARKER_COLORS[marker.markerColor] || '#1d385a';

            var circle = L.circleMarker([marker.latitude, marker.longitude], {
                radius: 9,
                color: '#ffffff',
                weight: 2,
                fillColor: color,
                fillOpacity: 0.95
            }).addTo(map);

            circle.bindPopup(buildPopupHtml(marker), { minWidth: 220 });
            bounds.push([marker.latitude, marker.longitude]);
        });

        if (bounds.length > 1) {
            map.fitBounds(bounds, { padding: [40, 40] });
        } else if (bounds.length === 1) {
            map.setView(bounds[0], 12);
        }
    }

    function init() {
        var mapEl = document.getElementById('leafletMap');
        if (!mapEl || typeof L === 'undefined') return;

        var map = L.map(mapEl, { zoomControl: true }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        setTimeout(function () { map.invalidateSize(); }, 0);
        window.addEventListener('resize', function () { map.invalidateSize(); });

        fetch('/Map/Data')
            .then(function (response) {
                if (!response.ok) throw new Error('Map data request failed: ' + response.status);
                return response.json();
            })
            .then(function (markers) {
                plotMarkers(map, markers || []);
            })
            .catch(function (err) {
                console.error('Could not load map data', err);
            });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();