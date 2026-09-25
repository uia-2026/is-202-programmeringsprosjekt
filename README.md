# IS-202 Programmeringsprosjekt

## 1. Drift (Docker)

### Bygge image
docker build -t is202-app .

### Kjør container
docker run -p 8080:8080 is202-app

### Miljøkrav
- Docker Desktop
- .NET 8 SDK
- Windows / macOS / Linux

### Stoppe container
docker ps
docker stop <container-id>

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
- Del 6: Dokumentasjon i GitHub (Najeebullah Maroof)
- Del 7: Dokumentasjon i selve koden (Danylo Bodnar)
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
