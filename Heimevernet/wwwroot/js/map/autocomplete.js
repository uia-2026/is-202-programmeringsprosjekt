import { CONFIG } from "./config.js";

/**
 * Global autocomplete for needs, resources, and addresses.
 *
 * Calls onSelect({ type, label, lat, lng }) for all result types.
 * The caller (e.g. location.js or main.js) decides what to do with the selection.
 */
export async function fetchAddresses(query, signal, limit = 6) {
    const params = new URLSearchParams({
        sok: query,
        treffPerSide: String(limit),
        fuzzy: "true"
    });

    const response = await fetch(`${CONFIG.geocodeUrl}?${params}`, {
        headers: { Accept: "application/json" },
        signal
    });

    if (!response.ok) {
        throw new Error(`Address search failed: ${response.status}`);
    }

    const data = await response.json();

    return (data.adresser ?? [])
        .filter(a =>
            Number.isFinite(Number(a.representasjonspunkt?.lat)) &&
            Number.isFinite(Number(a.representasjonspunkt?.lon))
        )
        .map(a => ({
            type: "address",
            label: [
                a.adressetekst,
                [a.postnummer, a.poststed].filter(Boolean).join(" ")
            ].filter(Boolean).join(", "),
            lat: Number(a.representasjonspunkt.lat),
            lng: Number(a.representasjonspunkt.lon)
        }));
}

/** Searches application needs and resources. */
export async function fetchDatabaseResults(query, signal, limit = 10) {
    const params = new URLSearchParams({
        q: query,
        limit: String(limit)
    });

    const response = await fetch(`/api/search?${params}`, {
        headers: { Accept: "application/json" },
        signal
    });

    if (!response.ok) {
        throw new Error(`Database search failed: ${response.status}`);
    }

    const data = await response.json();

    return (data.results ?? []).map(item => ({
        type: String(item.type).toLowerCase(),
        label: item.title,
        subtitle: item.subtitle,
        lat: item.latitude,
        lng: item.longitude,
        url: item.url,
        id: item.id
    }));
}

/**
 * Adds global autocomplete to a text input.
 *
 * Search priority:
 * 1. Needs
 * 2. Resources
 * 3. Norwegian addresses
 *
 * Supports debouncing, keyboard navigation (arrows, Enter, Escape)
 * and click selection.
 *
 * Calls onSelect({ type, label, lat, lng }) for addresses.
 * Navigates to the details page for needs and resources.
 */
export function initAutocomplete(input, onSelect) {
    const wrapper = input.parentElement;

    if (getComputedStyle(wrapper).position === "static") {
        wrapper.style.position = "relative";
    }

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
    let itemsFor = "";
    let active = -1;
    let timer = null;
    let controller = null;

    function setOpen(open) {
        list.style.display = open ? "block" : "none";
        input.setAttribute("aria-expanded", String(open));
    }

    function render(results, query, message = "No matching results found.") {
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
                rowEl.setAttribute("aria-selected", "false");

                const typeEl = document.createElement("span");
                typeEl.className = `app-search-result-type ${item.type}`;

                typeEl.textContent = {
                    address: "Address",
                    need: "Need",
                    resource: "Resource"
                }[item.type] ?? "Result";

                const contentEl = document.createElement("div");
                contentEl.className = "app-search-result-content";

                const titleEl = document.createElement("div");
                titleEl.className = "app-search-result-title";
                titleEl.textContent = item.label;

                contentEl.append(titleEl);

                if (item.subtitle) {
                    const subtitleEl = document.createElement("div");
                    subtitleEl.className = "app-search-result-subtitle";
                    subtitleEl.textContent = item.subtitle;

                    contentEl.append(subtitleEl);
                }

                rowEl.append(typeEl, contentEl);

                rowEl.addEventListener("click", () => choose(item));

                list.append(rowEl);
            });
        }

        setOpen(true);
    }

    function highlight(index) {
        const rows = list.querySelectorAll(".app-search-suggestion-item");

        rows.forEach((row, i) => {
            const selected = i === index;

            row.classList.toggle("active", selected);
            row.setAttribute("aria-selected", String(selected));
        });

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
        controller?.abort();
        controller = new AbortController();

        const signal = controller.signal;

        const [addressResult, databaseResult] = await Promise.allSettled([
            fetchAddresses(query, signal),
            fetchDatabaseResults(query, signal)
        ]);

        // Ignore results from outdated requests.
        if (signal.aborted || input.value.trim() !== query) {
            return;
        }

        const addresses = addressResult.status === "fulfilled"
            ? addressResult.value
            : [];

        const databaseResults = databaseResult.status === "fulfilled"
            ? databaseResult.value
            : [];

        const bothFailed =
            addressResult.status === "rejected" &&
            databaseResult.status === "rejected";

        if (bothFailed) {
            console.error("Global search failed:", {
                address: addressResult.reason,
                database: databaseResult.reason
            });

            render([], query, "Search is unavailable right now.");
            return;
        }

        if (addressResult.status === "rejected") {
            console.error("Address search failed:", addressResult.reason);
        }

        if (databaseResult.status === "rejected") {
            console.error("Database search failed:", databaseResult.reason);
        }

        // Sort database results: Needs first, then Resources.
        const sortedDatabaseResults = [...databaseResults].sort((a, b) => {
            const priority = {
                need: 0,
                resource: 1
            };

            return (priority[a.type] ?? 2) - (priority[b.type] ?? 2);
        });

        // Display database results before addresses.
        render([...sortedDatabaseResults, ...addresses], query);
    }

    async function submit() {
        window.clearTimeout(timer);

        const query = input.value.trim();

        // Use existing suggestions if they belong to the current query.
        if (items.length > 0 && itemsFor === query) {
            choose(items[active >= 0 ? active : 0]);
            return;
        }

        if (query.length < CONFIG.autocompleteMinChars) {
            return;
        }

        // Perform a fresh search for both providers.
        await search(query);

        // Only select results if the query is still current.
        if (input.value.trim() !== query) {
            return;
        }

        if (items.length > 0 && itemsFor === query) {
            choose(items[0]);
        }
    }

    input.addEventListener("input", () => {
        window.clearTimeout(timer);

        // Abort previous requests and invalidate existing suggestions.
        controller?.abort();
        items = [];
        itemsFor = "";
        active = -1;

        const query = input.value.trim();

        if (query.length < CONFIG.autocompleteMinChars) {
            setOpen(false);
            return;
        }

        timer = window.setTimeout(
            () => search(query),
            CONFIG.autocompleteDebounceMs
        );
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
        if (!wrapper.contains(event.target)) {
            setOpen(false);
        }
    });
}