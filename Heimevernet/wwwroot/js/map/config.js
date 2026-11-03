export const CONFIG = {
    dataUrl: "/Map/Data",
    geocodeUrl: "https://ws.geonorge.no/adresser/v1/sok",
    initialCenter: [58.35, 7.6],
    initialZoom: 9,
    maxZoom: 19,
    requestDebounceMs: 250,
    autocompleteDebounceMs: 300,
    autocompleteMinChars: 3,
    defaultRadiusKm: 10, // matches the selected option in the radius dropdown
    defaultBaseLayerName: "Standard map",
    tileUrl: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
    tileAttribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    baseLayers: {
        standard: {
            name: "Standard map",
            url: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            options: {
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            }
        },
        norge: {
            name: "Norgeskart (GeoNorge)",
            url: "https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png",
            options: {
                maxZoom: 18,
                attribution: 'Kartdata © <a href="https://www.kartverket.no/">Kartverket</a> / GeoNorge'
            }
        },
        aerial: {
            name: "Esri World Imagery",
            url: "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
            options: {
                maxZoom: 19,
                attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community'

            }
        }
    },

    focusZoom: 15,
    addressZoom: 14,
};


export function createBaseLayers() {
    const layers = {};

    Object.entries(CONFIG.baseLayers).forEach(([key, layerDef]) => {
        if (!layerDef) {
            return;
        }

        const layer = L.tileLayer(layerDef.url, layerDef.options);

        if (layer) {
            layers[layerDef.name] = layer;
        }
    });

    return layers;
}

export const PRIORITIES = ["Low", "Medium", "Critical"];

// Same colours as the legend in Index.cshtml
export const COLORS = {
    Low: "#2563eb",
    Medium: "#ca8a04",
    Critical: "#dc2626",
    resourceAvailable: "#16a34a",
    resourceBusy: "#64748b",
    search: "#7c3aed",
    user: "#0ea5e9"
};

