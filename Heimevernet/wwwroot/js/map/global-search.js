import { initAutocomplete } from "./autocomplete.js";

const input = document.getElementById("globalSearchInput");

// On the map page, main.js wires the box itself, because it can move the map without a reload
if (input && !document.getElementById("leafletMap")) {
    initAutocomplete(input, item => {
        const params = new URLSearchParams({ lat: item.lat, lng: item.lng });
        if (item.type === "address") params.set("label", item.label);
        else params.set("focus", `${item.type}:${item.id}`);
        window.location.assign(`/Map?${params}`);
    });
}