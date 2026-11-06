# Security testing

For each attack named in the assignment, this page shows why the application is protected.

## SQL injection

### Why the application is protected

- **Only EF Core LINQ, no raw SQL.** The code base has no `FromSqlRaw`, `ExecuteSqlRaw`, `SqlQuery`, `DbCommand`, `MySqlCommand` or `migrationBuilder.Sql` calls. User input only appears as C# values in LINQ, for example `_context.Resources.Where(r => r.Title.Contains(query))` in `SearchRepository`.
- **Input is always sent as parameters.** EF Core sends every C# value in a query as a SQL parameter, separate from the SQL text. The SQL text is fixed in code, so input can never change it.
- **No column names from the user.** Sorting uses the `ResourceSortOption` enum mapped to fixed `OrderBy` calls in a `switch`.
- **Typed input.** Ids, quantities, coordinates and dates are bound as `int`, `double` and `DateTime`, so text like `1 OR 1=1` is rejected by model binding before a query is built.
- **Length limits.** The search API (`SearchController`) accepts 2–100 characters and at most 20 results.

## Cross-site scripting (XSS)

### Why the application is protected

- **Razor encodes all output.** Every `@value` in a view is HTML-encoded, so `<script>` is rendered as the text `&lt;script&gt;`. No view uses `Html.Raw`, `HtmlString` or `Json.Serialize` to output raw HTML.
- **Map popups escape every user field.** The map builds Leaflet popups from strings in `wwwroot/js/map/markers.js`. Title, category, region, status and dates go through `escapeHtml` (`wwwroot/js/map/utils.js`), which replaces `& < > " '`. The details link uses `encodeURIComponent` on the id, and colours come from fixed constants, not from user data.
- **Other scripts insert text, not HTML.** The search suggestions (`autocomplete.js`) and the custom dropdown (`custom-select.js`) build elements with `createElement` and set `textContent`. The only `innerHTML` is `panel.innerHTML = ""`, which clears a list.
- **No user data in attributes or scripts.** `style` values come from enums (`AccentFor`). Image `src` values are built on the server from the bucket URL and a random GUID key. No inline `<script>` contains model data.
- **Uploads cannot carry scripts.** Only `.jpg`, `.jpeg`, `.png` and `.webp` are accepted, the file's first bytes must match the image format (`FileValidation`), and the server sets the content type. SVG and HTML files, which can contain scripts, are rejected.
- **The login cookie is `HttpOnly`.** Even if a script did run, it could not read the session cookie (`AuthenticationExtensions`).

## Content Security Policy

In production, Caddy adds a Content-Security-Policy header to every response (`Caddyfile`). The header limits which hosts the browser may load content from:

```text
default-src 'self';
script-src 'self' 'unsafe-inline' https://unpkg.com;
style-src 'self' 'unsafe-inline' https://unpkg.com https://cdn.jsdelivr.net;
img-src 'self' data: https://*.supabase.co https://unpkg.com https://*.tile.openstreetmap.org https://cache.kartverket.no https://server.arcgisonline.com;
font-src 'self' https://cdn.jsdelivr.net;
connect-src 'self' https://ws.geonorge.no;
object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'
```

### What it protects

- **Scripts only from known hosts.** A `<script src="https://attacker.example/...">` is blocked, because only this site and the Leaflet CDN are allowed.
- **No `eval`.** `'unsafe-eval'` is not allowed, so text cannot be run as code.
- **Data can only be sent to known hosts.** `connect-src` only allows this site and the Kartverket address search, so a script cannot send data to another server with `fetch`.
- **Images only from known hosts.** Map tiles, the photo bucket on Supabase, the Leaflet marker icons and `data:` URIs for the two-factor QR code.
- **Other attacks blocked as well.** `object-src 'none'` blocks plugins. `base-uri 'self'` stops a `<base>` tag from redirecting relative links. `form-action 'self'` stops forms posting to other sites. `frame-ancestors 'none'` stops other sites from showing the application in a frame (clickjacking).

**Trade-off:** `script-src` includes `'unsafe-inline'`, because several views use inline `<script>` blocks and `onchange`/`onsubmit` attributes. This means the policy does **not** block an injected inline script such as `<img onerror=...>`. Protection against that comes from output encoding (see [XSS](#cross-site-scripting-xss)). Removing `'unsafe-inline'` would require a nonce generated per request by the application and moving all inline handlers into script files. Caddy sends one fixed header, so it cannot provide a nonce. The header is only added in production, not when running locally.

## Cross-site request forgery (CSRF)

### Why the application is protected

- **Every form action checks an anti-forgery token.** All 16 `[HttpPost]` actions have `[ValidateAntiForgeryToken]`. That covers login, registration, logout, two-factor, creating, editing and deleting needs and resources, proposing, accepting and rejecting matches, and uploading and deleting photos. A request without a valid token is rejected with `400 Bad Request` before the action runs.
- **Forms get the token automatically.** Every `<form>` uses the `asp-action` tag helper, which adds the hidden `__RequestVerificationToken` field. A form on another site cannot read or guess the token.
- **GET requests never change data.** State changes only happen through POST, PUT and DELETE. Logout, for example, only works as a POST, so a link or `<img>` tag on another site cannot log a user out.
- **The JSON API is protected by the browser's cross-origin rules.** `PUT` and `DELETE` on `/api/needs/{id}` and `/api/resources/{id}` do not check a token. Other sites still cannot call them: an HTML form can only send GET and POST, and a cross-site `fetch` with `PUT`, `DELETE` or a JSON body needs a CORS preflight, which fails because the application has no CORS policy.
- **The login cookie is `SameSite=Lax`, `Secure` and `HttpOnly`** (`AuthenticationExtensions`). The browser does not send it with cross-site POST, PUT or DELETE requests, so even a forged request would arrive without a logged-in user.
- **Every page requires login by default.** A fallback authorisation policy requires an authenticated user, so a forged request without the cookie is redirected to the login page.
