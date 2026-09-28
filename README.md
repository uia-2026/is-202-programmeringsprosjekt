# IS-202 Programmeringsprosjekt

## 1. Drift

### Miljøkrav
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (må være startet)
- [.NET SDK](https://dotnet.microsoft.com/download) (samme versjon som i `Heimevernet.csproj`)
- Git

### Kom i gang

1. Klon repoet:
   ```bash
   git clone https://github.com/uia-2026/is-202-programmeringsprosjekt.git
   cd is-202-programmeringsprosjekt
   ```
2. Start Docker Desktop og vent til den er klar.
3. Start prosjektet:
   ```bash
   dotnet run --project Heimevernet.AppHost
   ```
4. Aspire-dashboardet åpnes i nettleseren. Klikk på lenken til webapplikasjonen for å åpne siden. Portnummeret varierer og står i dashboardet.

Alternativt: åpne `Heimevernet.slnx` i Visual Studio eller Rider, sett `Heimevernet.AppHost` som startprosjekt og trykk F5.

### Kjøreinstruksjon

Prosjektet kjøres med .NET Aspire. `Heimevernet.AppHost` starter alle delene (MariaDB, Migrator og webapplikasjonen). Du starter altså ikke webprosjektet direkte.

For å stoppe: trykk `Ctrl + C` i terminalen der `dotnet run` kjører. Aspire stopper da containerne.

### Databaseoppsett (MariaDB)

Du trenger ikke installere eller sette opp databasen selv.

- Aspire starter MariaDB som en Docker-container når AppHost kjører.
- `Heimevernet.Migrator` kjører databasemigreringene automatisk og oppretter tabellene.
- Migratoren legger også inn testdata (seed-data) i utviklingsmiljø.
- Connection string settes av Aspire, så ingen manuell konfigurasjon er nødvendig.

Vent til `Migrator` står som ferdig i dashboardet før du åpner nettsiden, ellers kan siden vise tomme data.

### Feilsøking

| Problem | Løsning |
|---|---|
| Feil om Docker eller container | Docker Desktop er ikke startet |
| Feil om at SDK ikke finnes | Installer riktig .NET-versjon |
| Tom side eller ingen data | Vent til Migrator er ferdig i dashboardet |
| `git` eller `dotnet` gjenkjennes ikke | Start terminalen på nytt etter installasjon |

---

## 2. Systemarkitektur

Prosjektet bruker ASP.NET Core MVC.

- Controller  
  Håndterer GET og POST. Tar imot data fra skjema og sender data til views.

- ViewModel  
  Transportobjekt mellom controller og view.

- View (Razor)  
  Viser dynamisk innhold fra serveren.

- Kart  
  Brukeren velger posisjon. Koordinater sendes til serveren og vises på en annen side.

- Docker  
  Applikasjonen kjører i container. Port 8080 eksponeres.

### Dataflyt
1. Bruker åpner skjema (GET).
2. Bruker fyller inn skjema + kartposisjon.
3. POST sender data til serveren.
4. Server lagrer data midlertidig.
5. En annen side viser dataene.

---

## 3. Testing

### Testscenarier
- GET: Skjema vises riktig.
- POST: Data sendes inn og vises på ny side.
- Kart: Klikk på kart gir korrekte koordinater.
- Responsivt design: Testet på mobil, tablet og desktop.
- Docker: Container starter uten feil og applikasjonen er tilgjengelig på localhost:8080.

### Testresultater
(Fylles inn når applikasjonen er ferdig.)

---

## 4. Kodedokumentasjon

- XML-kommentarer på controllere.
- Kommentarer i ViewModels.
- Ryddig struktur i Views.
- Kommentarer i Dockerfile.

---

## 5. Gruppe og leveranse

GitHub-lenke: https://github.com/uia-2026/is-202-programmeringsprosjekt

### Roller
- Del 1: Controller, ViewModel og View
- Del 2: Responsive nettsider med dynamisk innhold (Daniel Nemeye)
- Del 3: Håndtering av GET og POST forespørsler (Helal Karokhel)
- Del 4: Skjema som tar data fra brukeren og visning på annen side (Sebastian Ruben Van Est)
- Del 5: Kart + hente data fra kartet og vise på annen side (Rune Johan Corne Liefting)
- Del 6: Dokumentasjon i GitHub ()
- Del 7: Dokumentasjon i selve koden (Najeebullah Maroof)
- Del 8: Dokumenter deres bruk av KI i prosjektet fra ide til koding i Github READ ME (bruksområder, verktøy, prompt kommandoer).
Vi vil bare lære om hvordan dere har brukt KI. Dette skal ikke påvirke godkjenningen i det hele tatt.

---

## 6. Bruk av KI i prosjektet

Vi har brukt KI som støtteverktøy i prosjektet. Her dokumenterer vi hvordan KI ble brukt fra idé til ferdig kode.

### Bruksområder
- Må fylles inn

### Verktøy
- Microsoft Copilot  
- GitHub Copilot  
- KI‑assistent (ChatGPT‑lignende verktøy)

### Eksempler på prompt‑kommandoer
- Må fylles inn

### Hva KI ikke gjorde
- Må fylles inn

### Oppsummering
KI ble brukt til planlegging, dokumentasjon, feilsøking og inspirasjon. All implementasjon ble gjort av gruppen.
