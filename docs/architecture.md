# Systemarkitektur

## Overordnet struktur

```
┌─────────────────┐
│   Bruker        │
│  (Web Browser)  │
└────────┬────────┘
         │ HTTP GET/POST
         ▼
┌─────────────────────────────┐
│  ASP.NET Core MVC App       │
├─────────────────────────────┤
│  Views (Razor)              │
│  - Skjema (form)            │
│  - Kartvisning              │
│  - Resultatside             │
└────────┬────────────────────┘
         │
┌────────▼────────────────────┐
│  Controllers                │
│  - GET: Viser skjema        │
│  - POST: Mottar data        │
└────────┬────────────────────┘
         │
┌────────▼────────────────────┐
│  ViewModels                 │
│  - Transportobjekter        │
│  - Data mellom View/Control │
└────────┬────────────────────┘
         │
┌────────▼────────────────────┐
│  Midlertidig lagring        │
│  - Session/Memory           │
└─────────────────────────────┘
```

## Dataflyt

1. Bruker åpner siden (GET)**
    Browser sender GET-request til Controller
    Controller returnerer View med tomt skjema

2. Bruker fyller inn og velger kartposisjon**
    Bruker skriver inn data
   Bruker klikker på kart → koordinater registreres
   -Bruker klikker "Send"

3. Data sendes (POST)**
   Skjema sender POST-request med data + koordinater
   Controller mottar data via ViewModel

4. **Server lagrer data midlertidig**
    Data lagres i session/memory
   Bruker omdirigeres til resultatside

5. **Resultatside vises**
   - View viser de innsendte dataene
   - Data vises i tabell/kort format

## Teknologi
 **Kjøringsmiljø:** ASP.NET Core 8 (MVC)
**Frontend:** HTML, CSS, JavaScript (Razor)
**Backend:** C#
 **Containerisering:** Docker
**Port:** 8080
