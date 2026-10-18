# Map

The map page shows needs and resources on a Leaflet map. Marker data is loaded for the visible area only, and the server decides what each user may see.

## What is shown

| Marker                  | Shape            | Colour |
| ----------------------- | ---------------- | ------ |
| Need, priority Critical | Diamond with `!` | Red    |
| Need, priority Medium   | Diamond with `M` | Yellow |
| Need, priority Low      | Diamond with `L` | Blue   |
| Resource, available     | Square with `R`  | Green  |
| Resource, busy          | Square with `R`  | Grey   |

The shape tells needs from resources even without colour. A popup shows the title, category, region, deadline or availability, distance from the search point (when one is set), a link to the details page, and further details on request.

## Who sees what

| Role              | Needs          | Resources                |
| ----------------- | -------------- | ------------------------ |
| Public actor      | All unresolved | Available resources only |
| Resource provider | All unresolved | Their own resources only |

Providers still see needs, because they act on them. The rules run on the server in `MapService`, so a user cannot see more by calling the endpoint directly.

## Data endpoint

`GET /Map/Data?south=..&north=..&west=..&east=..`

The bounds are validated: all four are required, latitudes lie within -90 and 90, longitudes within -180 and 180, and south must not exceed north. Invalid input returns `400`.

```json
{
  "markers": [
    {
      "id": 12,
      "kind": "Need",
      "title": "Snow removal",
      "category": "Machinery",
      "description": "...",
      "region": "Agder",
      "priority": "Critical",
      "status": "New",
      "when": "2026-11-02T08:00:00Z",
      "contactInfo": "...",
      "latitude": 58.15,
      "longitude": 7.99
    }
  ],
  "truncated": false
}
```

The server returns at most 500 markers per kind. It asks the database for one more than that, and sets `truncated` when the extra row comes back. The page then asks the user to zoom in. Needs are ordered so that, if a cut happens, critical needs are kept first.

Queries filter by bounds, status and owner in SQL, use no change tracking and select only the columns a marker needs.

## Filters and search

| Control                      | Behaviour                                                                                                                                    |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Type                         | Show needs, resources or both                                                                                                                |
| Need priority                | Low, Medium, Critical                                                                                                                        |
| Category, region             | Options follow the markers currently loaded                                                                                                  |
| Need status, resource status | Fixed lists                                                                                                                                  |
| Address search               | Type at least three characters for suggestions from Geonorge. Arrow keys, Enter and Escape work, and choosing an address moves the map there |
| Use my location              | Asks the browser for permission, marks the position with a blue dot and draws the search radius                                              |
| Radius                       | 5, 10, 25 or 50 km around the search point, applied in the browser to the loaded markers                                                     |
| Reset                        | Clears filters and the search point                                                                                                          |

Location needs a secure context (HTTPS or localhost). Denied permission, an unavailable position and a timeout each show their own message.

## Code structure

The map is plain ES modules, with no bundler. `Index.cshtml` loads Leaflet as a normal script, then `main.js` as a module, which imports the rest.

| File in `wwwroot/js/map` | Responsibility                                                      |
| ------------------------ | ------------------------------------------------------------------- |
| `config.js`              | Constants and colours                                               |
| `state.js`               | Shared state and cached page elements                               |
| `utils.js`               | Escaping, formatting, coordinates and distance                      |
| `markers.js`             | Icons, popups and rendering                                         |
| `filters.js`             | Filter controls and the filtering rules                             |
| `data.js`                | Loads markers for the visible bounds, ignores out-of-date responses |
| `autocomplete.js`        | Address suggestions                                                 |
| `location.js`            | Address search, geolocation and the search circle                   |
| `main.js`                | Creates the map and connects the modules                            |

Styles are in `wwwroot/css/map.css`. All text from the database is escaped before it is placed in a popup, because titles and descriptions are typed by users.

## Limitations

- The radius filter only sees markers loaded for the current view. If the radius is larger than the visible area, zoom out.
- Address search covers Norwegian addresses (Geonorge).
- Only the first 500 markers of each kind are shown in very busy areas.
