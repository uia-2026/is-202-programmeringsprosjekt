import { CONFIG } from "./config.js";

/** Looks up Norwegian addresses and returns [{ label, lat, lng }]. */
export async function fetchAddresses(query, signal, limit = 6) {
    const params = new URLSearchParams({ sok: query, treffPerSide: String(limit), fuzzy: "true" });
    const response = await fetch(`${CONFIG.geocodeUrl}?${params}`, {
        headers: { Accept: "application/json" },
        signal
    });
    if (!response.ok) throw new Error(`Address search failed: ${response.status}`);

    const data = await response.json();
    return (data.adresser ?? [])
        .filter(a => Number.isFinite(Number(a.representasjonspunkt?.lat)) &&
                     Number.isFinite(Number(a.representasjonspunkt?.lon)))
        .map(a => ({
            label: [a.adressetekst, [a.postnummer, a.poststed].filter(Boolean).join(" ")]
                .filter(Boolean).join(", "),
            lat: Number(a.representasjonspunkt.lat),
            lng: Number(a.representasjonspunkt.lon)
        }));
}

/**
 * Adds an address dropdown to a text input: debounced lookups, keyboard navigation
 * (arrows, Enter, Escape) and click selection. Calls onSelect({ label, lat, lng }).
 */
export function initAutocomplete(input, onSelect) {
    const wrapper = input.parentElement;
    if (getComputedStyle(wrapper).position === "static") wrapper.style.position = "relative";

    const list = document.createElement("div");
    list.id = "addressSuggestions";
    list.className = "app-search-suggestions";
    list.setAttribute("role", "listbox");
    list.style.display = "none";
    wrapper.appendChild(list);

    input.setAttribute("autocomplete", "off");
    input.setAttribute("role", "combobox");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-controls", list.id);
    input.setAttribute("aria-expanded", "false");

    let items = [];
    let itemsFor = "";      // the query the current items belong to
    let active = -1;
    let timer = null;
    let controller = null;

    function setOpen(open) {
        list.style.display = open ? "block" : "none";
        input.setAttribute("aria-expanded", String(open));
    }

    function render(results, query, message = "No matching address found.") {
        items = results;
        itemsFor = query;
        active = -1;
        input.removeAttribute("aria-activedescendant");
        list.replaceChildren();

        if (results.length === 0) {
            const empty = document.createElement("div");
            empty.className = "app-search-suggestion-empty";
            empty.textContent = message;
            list.append(empty);
        } else {
            results.forEach((item, index) => {
                const rowEl = document.createElement("div");
                rowEl.id = `address-option-${index}`;
                rowEl.className = "app-search-suggestion-item";
                rowEl.setAttribute("role", "option");
                rowEl.textContent = item.label;   // textContent: never HTML
                rowEl.addEventListener("click", () => choose(item));
                list.append(rowEl);
            });
        }
        setOpen(true);
    }

    function highlight(index) {
        const rows = list.querySelectorAll(".app-search-suggestion-item");
        rows.forEach((r, i) => r.classList.toggle("active", i === index));
        active = index;
        if (rows[index]) {
            rows[index].scrollIntoView({ block: "nearest" });
            input.setAttribute("aria-activedescendant", rows[index].id);
        }
    }

    function choose(item) {
        input.value = item.label;
        setOpen(false);
        onSelect(item);
    }

    async function search(query) {
        controller?.abort();                      // a newer keystroke replaces the old request
        controller = new AbortController();
        try {
            render(await fetchAddresses(query, controller.signal), query);
        } catch (error) {
            if (error.name === "AbortError") return;
            console.error("Address autocomplete failed:", error);
            render([], query, "Address search is unavailable right now.");
        }
    }

    async function submit() {
        window.clearTimeout(timer);
        const query = input.value.trim();

        // Only trust the dropdown if it belongs to what is currently typed
        if (items.length > 0 && itemsFor === query) {
            choose(items[active >= 0 ? active : 0]);
            return;
        }
        if (query.length < CONFIG.autocompleteMinChars) return;

        try {
            const [first] = await fetchAddresses(query, undefined, 1);
            if (first) choose(first);
            else render([], query);
        } catch (error) {
            console.error("Address search failed:", error);
            render([], query, "Address search is unavailable right now.");
        }
    }

    input.addEventListener("input", () => {
        window.clearTimeout(timer);
        const query = input.value.trim();

        if (query.length < CONFIG.autocompleteMinChars) {
            controller?.abort();
            setOpen(false);
            return;
        }
        timer = window.setTimeout(() => search(query), CONFIG.autocompleteDebounceMs);
    });

    input.addEventListener("keydown", event => {
        switch (event.key) {
            case "ArrowDown":
            case "ArrowUp": {
                if (items.length === 0) return;
                event.preventDefault();
                const step = event.key === "ArrowDown" ? 1 : -1;
                highlight((active + step + items.length) % items.length);
                break;
            }
            case "Enter":
                event.preventDefault();
                submit();
                break;
            case "Escape":
                setOpen(false);
                break;
        }
    });

    document.addEventListener("click", event => {
        if (!wrapper.contains(event.target)) setOpen(false);
    });
}