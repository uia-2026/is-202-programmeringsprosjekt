export const state = {
    map: null,
    markerLayer: null,
    searchLayer: null,
    allMarkers: [],
    hasLoadedMarkers: false,
    isLoading: false,
    searchLocation: null // { lat, lng, label }
};

export const elements = {};

const ELEMENT_IDS = [
    "mapFilterBox", "mapFilterToggle", "mapFilterCategory", "mapFilterRegion",
    "mapFilterNeedStatus", "mapFilterResourceStatus", "mapFilterRadius",
    "mapFilterLocationBtn", "mapFilterDistanceNote", "mapFilterResetBtn",
    "mapEmptyNote", "mapCapNote", "globalSearchInput"
];

export function cacheElements() {
    ELEMENT_IDS.forEach(id => { elements[id] = document.getElementById(id); });
}