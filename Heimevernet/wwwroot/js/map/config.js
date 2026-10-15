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
    tileUrl: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
    tileAttribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
};

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