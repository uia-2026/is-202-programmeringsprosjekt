# Bruk av KI i prosjektet

Dette dokumentet beskriver hvordan gruppen brukte kunstig intelligens (KI) som
støtte gjennom prosjektet. KI ble brukt som et hjelpemiddel, mens gruppen selv
vurderte forslagene, skrev om det som var nødvendig og testet løsningen.

## Verktøy

- Microsoft Copilot til idéutvikling, forklaringer og feilsøking.
- GitHub Copilot til forslag mens vi skrev C#, Razor, SQL/EF Core og Markdown.
- ChatGPT-lignende KI-assistent til planlegging, dokumentasjon og spørsmål om
  ASP.NET Core, .NET Aspire, MariaDB og Docker.

## Bruksområder

### Design- og planleggingsfase

KI ble brukt til å diskutere mulige løsninger før implementasjonen startet.
Vi brukte blant annet KI til å:

- sammenligne ASP.NET Core MVC med andre mulige oppsett og begrunne hvorfor
  MVC passet til skjemaer, controllere og Razor Views;
- foreslå en lagdeling med Controller, ViewModel, Service, Mapper, Repository,
  Unit of Work og database;
- diskutere hvilke entiteter og relasjoner som trengtes for `Need`, `Resource`,
  `Category`, `Attachment`, `Match` og `User`;
- få forslag til hvordan et ER-diagram og et arkitekturdiagram kunne bygges;
- planlegge request-flyten fra nettleser via controller og service til MariaDB.

KI-forslagene var utgangspunkt for diskusjon. Gruppen valgte selv hvilke
forslag som passet med oppgaven og den faktiske koden.

### Kodefase

KI ble brukt som støtte til å forstå og skrive deler av oppsettet, blant annet:

- grunnstruktur for MVC-controllere med GET- og POST-actions;
- ViewModels med validering og binding av skjemafelt;
- Razor Views, HTML og CSS for skjemaer og responsivt design;
- EF Core-entiteter, relasjoner, enum-konvertering og repository-mønster;
- .NET Aspire-oppsett for Web, Migrator og MariaDB;
- `dotnet user-secrets` for databasepassord som ikke skal ligge i GitHub;
- forklaring av feilmeldinger og forslag til feilsøking ved bygging og oppstart;
- forslag til unit-tester for service- og mapper-lagene.

Kodeforslag ble ikke tatt som fasit. Gruppen tilpasset forslagene til prosjektets
klassenavn, eksisterende arkitektur og krav, og kontrollerte at løsningen bygget
og fungerte sammen med resten av prosjektet.

### Dokumentasjonsfase

KI ble brukt til å:

- strukturere README-en og dokumentasjonen i `docs/`;
- forklare arkitektur, request flow og databaseforbindelse med enklere språk;
- forbedre formuleringer, overskrifter og Markdown-tabeller;
- lage forslag til diagrammer i Mermaid-format;
- finne dokumentasjonspunkter som måtte kontrolleres mot den faktiske koden.

Dokumentasjonen ble gjennomgått og korrigert av gruppen. Testresultater i
README skal fylles inn etter at applikasjonen er ferdig testet.

## Konkrete eksempler på prompter

Eksemplene under viser typen spørsmål vi brukte i arbeidet og hva KI ble brukt
til. De er dokumentert for å vise arbeidsprosessen, ikke for å påstå at KI
alene laget funksjonaliteten.

### Eksempel 1 – arkitektur

> **Prompt:** Vi lager en ASP.NET Core MVC-applikasjon med Need og Resource.
> Hvordan kan vi dele systemet i Controller, ViewModel, Service, Mapper,
> Repository, Unit of Work og MariaDB? Forklar også dataflyten for en POST.
>
> **Resultat:** KI foreslo en lagdelt request flow. Gruppen brukte dette som
> utgangspunkt, men tilpasset den til de faktiske klassene
> `NeedController`, `NeedService`, `NeedMapper`, `NeedRepository` og
> `AppDbContext`.

### Eksempel 2 – .NET Aspire og secrets

> **Prompt:** Hvordan kan et .NET Aspire AppHost bruke et databasepassord uten
> at passordet lagres i `appsettings.json` eller pushes til GitHub?
>
> **Resultat:** KI foreslo en Aspire-parameter som secret og bruk av
> `dotnet user-secrets`. Gruppen brukte dette som grunnlag for oppsettet og
> dokumenterte kommandoen i README-en.

### Eksempel 3 – GET og POST

> **Prompt:** Vis et eksempel på en ASP.NET Core MVC-controller der GET viser et
> skjema, POST validerer en ViewModel og ved ugyldige data viser skjemaet på
> nytt med valideringsfeil.
>
> **Resultat:** KI forklarte forskjellen mellom GET og POST, `ModelState.IsValid`
> og redirect etter vellykket lagring. Gruppen tilpasset mønsteret til Need- og
> Resource-controllerne og til de ViewModels som allerede finnes.

### Eksempel 4 – EF Core og database

> **Prompt:** Hvordan kan EF Core lagre enums som lesbare tekstverdier i en
> MariaDB-database, og hvordan kan en unik kombinasjon av `NeedId` og
> `ResourceId` håndheves?
>
> **Resultat:** KI foreslo `HasConversion<string>()` og en unik indeks. Gruppen
> kontrollerte forslaget mot `AppDbContext` og brukte det der det passet med
> datamodellen.

### Eksempel 5 – dokumentasjon og diagram

> **Prompt:** Lag et enkelt Mermaid-arkitekturdiagram for nettleser, ASP.NET
> Core MVC, Migrator og MariaDB, og vis hvilke komponenter AppHost starter.
>
> **Resultat:** KI laget et første diagramutkast. Gruppen fjernet generelle
> eller misvisende deler og beholdt et diagram som beskriver prosjektets
> faktiske AppHost-, Web-, Migrator- og MariaDB-komponenter.

## Hva KI ikke gjorde

- KI tok ikke selvstendige beslutninger om krav, arkitektur eller hvilke
  funksjoner som skulle leveres.
- KI hadde ikke tilgang til gruppens lokale kjøring, database eller
  utviklingsmiljø og kunne derfor ikke bekrefte at forslagene fungerte.
- KI skrev ikke den ferdige løsningen uten gjennomgang. Gruppen integrerte,
  tilpasset og kvalitetssikret kode i prosjektet.
- KI gjennomførte ikke den endelige testingen. Gruppen må selv kjøre
  `dotnet test`, starte applikasjonen og gjennomføre de manuelle testene.
- KI opprettet ikke testresultater. Resultater skal dokumenteres først når
  testene faktisk er gjennomført.
- KI fikk ikke legge inn hemmeligheter, passord eller personopplysninger i
  repositoryet.
- KI erstattet ikke code review eller samarbeidet i gruppen.

## Hvor mye ble tatt direkte vs. tilpasset

Det finnes ikke en nøyaktig prosentandel for hele prosjektet. Generelt ble
forklaringer, idéer og dokumentasjonsutkast ofte omskrevet og kontrollert mot
koden. Kodeforslag ble brukt som utgangspunkt, men ble tilpasset med prosjektets
egne navn, modeller, validering, feilhåndtering og struktur. Ingenting ble
ansett som ferdig bare fordi KI foreslo det; gruppen måtte selv forstå, bygge og
teste endringene.

## Oppsummering

KI ble brukt til planlegging, læring, dokumentasjon, feilsøking og inspirasjon.
Den viktigste bruken var å få forslag og forklaringer raskere, ikke å overlate
ansvaret for løsningen til KI. Gruppen har selv valgt arkitektur, tilpasset
implementasjonen, vurdert sikkerhet og gjennomført kvalitetssikring. KI var et
støtteverktøy, mens ansvar for kode og innhold ligger hos gruppen.
