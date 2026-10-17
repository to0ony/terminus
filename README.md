<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/logo-white.png">
    <img src="docs/logo.png" alt="Terminus" width="420">
  </picture>
</p>

<p align="center">
  Izrada, objava i prikaz voznih redova javnog prijevoza u formatu <a href="https://gtfs.org/">GTFS</a>.
</p>

<p align="center">
  <a href="https://github.com/to0ony/terminus/actions/workflows/ci.yml"><img src="https://github.com/to0ony/terminus/actions/workflows/ci.yml/badge.svg?branch=master" alt="Build & Test"></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/PostgreSQL-13%2B-4169E1?logo=postgresql&logoColor=white" alt="PostgreSQL 13+">
  <img src="https://img.shields.io/badge/GTFS-Schedule-0B7A75" alt="GTFS Schedule">
</p>

---

Terminus je aplikacija u kojoj prijevoznik vodi stanice, linije, kalendare i
vožnje, iz njih izvozi GTFS feed za Google Maps i druge aplikacije, a putnicima
prikazuje polaske na javnim stranicama stanica.

Terminus je **single-tenant**: svaka tvrtka (npr. gradski prijevoznik) dobiva
vlastitu instancu i bazu. Unutar instance može postojati više prijevoznika
(GTFS `agency`), a svaka linija pripada jednom od njih.

## Mogućnosti

**Za administratora (dashboard)**

- **Prvo pokretanje** – čarobnjak: uvoz postojećeg GTFS-a ili početak od nule.
- **GTFS uvoz** – pregled s izvještajem (greške, upozorenja, preskočene
  datoteke) pa uvoz; interpolacija praznih vremena, `frequencies.txt` u
  pojedinačne vožnje, nazivi kalendara; milijun vremena za pola minute.
- **GTFS izvoz** s provjerama prije izvoza i, po izboru, javni feed bez prijave.
- **Pregled voznog reda** – linije, vozni red linije po danu i smjeru, stanice
  na karti, polasci sa stanice za odabrani dan, kalendari s iznimkama,
  prijevoznici i pregled do kada vrijedi vozni red.
- **PDF vozni red stanice** – A4 ili A3, hrvatski ili engleski, za odabrano
  razdoblje (i unaprijed); stupci po danima u tjednu, praznici i odstupanja kao
  napomene, QR kod na zaslon stanice.
- **Postavke instance** – naziv, boje i logo za javne stranice i PDF, javna
  adresa, objava GTFS feeda, promjena lozinke.

**Za putnike**

- **Zaslon na stanici** – javna stranica `/board/{stopCode}` sa sljedećim
  polascima (linija, odredište, "za 4 min"), za ekrane na stanicama i mobitele.
- **Javni API** polazaka i GTFS feeda, s cacheom i ograničenjem zahtjeva.

**Temelji**

- **GTFS model** – sva polja osnovnih datoteka (`agency`, `stops`, `routes`,
  `trips`, `stop_times`, `calendar`, `calendar_dates`), stanice s peronima,
  ulazima i mjestima ukrcavanja, trase (`shapes`), cijene (Fares v1) i
  `feed_info`.
- **Izračun polazaka** – kalendari i iznimke, vožnje preko ponoći (do 48 h),
  ljetno računanje vremena, kolodvori s peronima.
- **Prijava administratora** kolačićem, uz zaključavanje računa nakon
  neuspjelih pokušaja.

U planu su PDF za sve stanice odjednom, editor voznog reda i produkcijski
deploy; vidi [roadmap](docs/roadmap.md).

## Tehnologije

| Dio | Tehnologije |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), Entity Framework Core 10, ASP.NET Core Identity |
| Baza | PostgreSQL (Npgsql, snake_case nazivi) |
| PDF | QuestPDF, QRCoder |
| Frontend | React 19, TypeScript, Vite, Mantine, React Router, TanStack Query, MapLibre GL |
| Testovi | xUnit v3 (backend), Vitest i Testing Library (frontend) |
| CI | GitHub Actions |

## Struktura repozitorija

```
terminus/
├── backend/                   .NET rješenje Terminus.slnx
│   ├── Terminus.WebApi/       kontroleri, ugovori (DTO), konfiguracija, prijava, javni API
│   ├── Terminus.Service*/     poslovna pravila, polasci, GTFS uvoz i izvoz, PDF
│   ├── Terminus.Repository*/  pristup podacima (EF Core)
│   ├── Terminus.DAL/          entiteti, konfiguracije i migracije
│   ├── Terminus.Model*/       modeli, sučelja, enumi i filteri
│   ├── Terminus.Common/       zajednički tipovi (GTFS vrijeme i datum, iznimke)
│   └── Terminus.Tests/        unit testovi
├── frontend/                  dashboard i javni zaslon stanice (React)
└── docs/                      dokumentacija
```

## Brzi početak

Preduvjeti: [.NET 10 SDK](https://dotnet.microsoft.com/download),
PostgreSQL 13+, `dotnet-ef` i [Node.js](https://nodejs.org) 22+.

```sh
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=terminus;Username=postgres;Password=<lozinka>" --project Terminus.WebApi
dotnet user-secrets set "Admin:Email" "admin@example.com" --project Terminus.WebApi
dotnet user-secrets set "Admin:Password" "<barem 10 znakova>" --project Terminus.WebApi
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi
dotnet run --project Terminus.WebApi --launch-profile http   # http://localhost:5183
```

```sh
cd frontend
npm install
npm run dev    # http://localhost:5173, /api ide na backend preko Vite proxyja
```

Nakon prijave čarobnjak nudi uvoz GTFS-a ili početak od nule. Konfiguracija,
testovi i migracije opisani su u [lokalnom razvoju](docs/development.md).

## Dokumentacija

| Dokument | Sadržaj |
|---|---|
| [Roadmap](docs/roadmap.md) | stanje, faze razvoja, odluke i otvorena pitanja |
| [Lokalni razvoj](docs/development.md) | postavljanje, konfiguracija, prvo pokretanje, testovi, migracije |
| [Arhitektura](docs/architecture.md) | backend slojevi, tok zahtjeva, obrada grešaka |
| [Frontend](docs/frontend.md) | stranice, struktura, rute i pozivi API-ja |
| [API](docs/api.md) | prijava, endpointi, javni API, greške |
| [Model podataka i GTFS](docs/data-model.md) | tablice, podržana GTFS polja, pravila |
| [GTFS uvoz](docs/gtfs-import.md) | izvještaj, greške i upozorenja, pretvorbe |
| [GTFS izvoz](docs/gtfs-export.md) | sadržaj feeda, format i provjere |
| [Polasci, zaslon i PDF](docs/departures-and-timetables.md) | izračun polazaka, zaslon na stanici, PDF vozni red |
| [Shema baze](docs/terminus_schema.sql) | SQL shema s komentarima |
