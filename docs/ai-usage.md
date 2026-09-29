# Bruk av KI i prosjektet

> **Status:** Utkast flyttet fra README. Må fylles ut av hele gruppa med
> konkrete eksempler, ikke bare generelle kategorier.

Vi har brukt KI som støtteverktøy i prosjektet. Her dokumenterer vi hvordan
KI ble brukt fra idé til ferdig kode.

## Verktøy

- Microsoft Copilot
- GitHub Copilot
- KI-assistent (ChatGPT-lignende verktøy / Claude)

## Bruksområder

TODO – fyll inn konkret, per fase:

- **Designfase:** f.eks. arkitekturvalg (MVC vs. React), diskusjon rundt
  databasedesign/ER-diagram.
- **Kodefase:** f.eks. scaffolding av modeller, EF Core-oppsett, feilsøking
  av Git/Docker/Aspire-oppsett.
- **Dokumentasjonsfase:** f.eks. strukturering av README og docs/-mappen.

## Eksempler på prompter

TODO – lim inn 3-5 konkrete eksempler på faktiske spørsmål/prompter som ble
stilt, og kort hva slags svar eller kode som kom ut. Eksempel på format:

> **Prompt:** "Hvordan setter jeg opp databasepassord i .NET Aspire slik at
> det ikke havner i repoet?"
> **Resultat:** Forslag om å bruke `dotnet user-secrets` og
> `builder.AddParameter(secret: true)`, som ble brukt i `AppHost.cs`.

## Hva KI ikke gjorde

TODO – vær konkret. Eksempel: KI skrev ikke selve forretningslogikken i
service-laget, men ble brukt til å foreslå struktur og feilsøke oppsett.

## Hvor mye ble tatt direkte vs. tilpasset

TODO – gi et ærlig anslag, f.eks.: dokumentasjonstekst ble i stor grad
generert og deretter korrigert manuelt mot faktisk kode; kodeforslag fra KI
ble i all hovedsak brukt som utgangspunkt og tilpasset av teamet.

## Oppsummering

KI ble brukt til planlegging, dokumentasjon, feilsøking og inspirasjon. All
implementasjon ble gjort av gruppen.
