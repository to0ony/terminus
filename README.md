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

- **GTFS model** – sva polja osnovnih datoteka (`agency`, `stops`, `routes`,
  `trips`, `stop_times`, `calendar`, `calendar_dates`), stanice s peronima,
  ulazima i mjestima ukrcavanja, trase (`shapes`), cijene (Fares v1) i
  `feed_info`.
- **GTFS izvoz** s provjerama prije izvoza i, po izboru, javni feed bez prijave.
- **Polasci** – sljedeći polasci sa stanice ili kolodvora (ponoć, vožnje do
  48 h, ljetno računanje vremena) za javne stranice i zaslone na stanicama.
- **Pregled voznog reda** – vozni red linije kao tablica stanica × vožnji,
  obrasci stanica, svi polasci sa stanice za odabrani dan, stanice na karti.
- **Prijava administratora** kolačićem, uz zaključavanje računa nakon
  neuspjelih pokušaja.
- **Postavke instance** – naziv, boje i logo za javne stranice i PDF.

U planu su uvoz GTFS-a, PDF vozni redovi, frontend (dashboard i zaslon na
stanici) i editor voznog reda; vidi [roadmap](docs/roadmap.md).

## Tehnologije

| Dio | Tehnologije |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), Entity Framework Core 10, ASP.NET Core Identity |
| Baza | PostgreSQL (Npgsql, snake_case nazivi) |
| Testovi | xUnit v3, Microsoft Testing Platform |
| CI | GitHub Actions |
| Frontend (u planu) | React, TypeScript, Vite |

## Struktura repozitorija

```
terminus/
├── backend/                   .NET rješenje Terminus.slnx
│   ├── Terminus.WebApi/       kontroleri, ugovori (DTO), konfiguracija i prijava
│   ├── Terminus.Service*/     poslovna pravila, izračun polazaka, GTFS izvoz
│   ├── Terminus.Repository*/  pristup podacima (EF Core)
│   ├── Terminus.DAL/          entiteti, konfiguracije i migracije
│   ├── Terminus.Model*/       modeli, sučelja, enumi i filteri
│   ├── Terminus.Common/       zajednički tipovi (GTFS vrijeme i datum, iznimke)
│   └── Terminus.Tests/        unit testovi
├── docs/                      dokumentacija
└── frontend/                  dashboard i zaslon na stanici (u planu)
```

Uz `Service`, `Repository` i `Model` postoji i projekt `*.Common` sa sučeljima
tog sloja. Slojevi su opisani u [arhitekturi](docs/architecture.md).

## Pokretanje

**Preduvjeti:** [.NET 10 SDK](https://dotnet.microsoft.com/download),
PostgreSQL 13+ i EF Core CLI (`dotnet tool install --global dotnet-ef`).

```sh
cd backend

# konfiguracija (User Secrets, izvan repozitorija)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=terminus;Username=postgres;Password=<lozinka>" --project Terminus.WebApi
dotnet user-secrets set "Admin:Email" "admin@example.com" --project Terminus.WebApi
dotnet user-secrets set "Admin:Password" "<barem 10 znakova>" --project Terminus.WebApi

# baza
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi

# API na http://localhost:5183 (OpenAPI: /openapi/v1.json)
dotnet run --project Terminus.WebApi --launch-profile http

# testovi
dotnet test --solution Terminus.slnx
```

Nakon prve prijave instanca još nema prijevoznika; njega stvara čarobnjak
prvog pokretanja (vidi [Prvo pokretanje](#prvo-pokretanje)). Primjeri zahtjeva su u
[`Terminus.WebApi.http`](backend/Terminus.WebApi/Terminus.WebApi.http); prvi je prijava. Više o lokalnom razvoju i migracijama u
[development.md](docs/development.md).

## Konfiguracija

Ključevi se zadaju kroz User Secrets, `appsettings` ili varijable okruženja
(`Admin__Email`, `ConnectionStrings__Default` …).

| Ključ | Obavezno | Opis |
|---|---|---|
| `ConnectionStrings:Default` | da | Connection string za PostgreSQL. |
| `Admin:Email` | za seed | E-mail (korisničko ime) administratora. |
| `Admin:Password` | za seed | Početna lozinka administratora, barem 10 znakova. |

**Administrator pri pokretanju.** Ako administrator s e-mailom `Admin:Email`
ne postoji, stvara se s lozinkom `Admin:Password`. Postojeći administratori se
nikad ne mijenjaju: promjena `Admin:Password` ne mijenja lozinku, a novi
administrator se dodaje promjenom `Admin:Email` i ponovnim pokretanjem.
Nepotpuna konfiguracija samo se zapisuje u log; aplikacija se normalno pokreće.

## Prvo pokretanje

Konfiguracija daje samo administratora. Nakon prve prijave dashboard prikazuje
čarobnjak (`GET /api/setup` vraća `completed: false`) s dva izbora:

- **uvoz postojećeg GTFS-a** – prijevoznici, stanice, linije i vozni red dolaze
  iz feeda; prvo se prikaže izvještaj provjere, a podaci se upisuju tek nakon
  potvrde (u izradi);
- **početak od nule** – `POST /api/setup/blank` stvara prvog prijevoznika
  (naziv i web adresa; kod, zona i jezik imaju zadane vrijednosti).

Kad je čarobnjak završen, ne prikazuje se ponovno ni ako se podaci kasnije
obrišu. Postavke instance (naziv, logo, boje) uređuju se naknadno.

## Prijava

Sve osim `/api/public/*`, `/api/auth/login` i `/health` traži prijavu
(inače `401`).

- Kolačić `terminus_auth`: HttpOnly, SameSite=Strict, u produkciji samo preko
  HTTPS-a; vrijedi 14 dana i produljuje se korištenjem.
- Nakon 5 neuspjelih pokušaja račun je zaključan 15 minuta (`429`).
- Ključevi za kolačiće čuvaju se u bazi, pa prijava preživi restart i deploy.
- Promjena lozinke (`PUT /api/auth/password`) odjavljuje ostale preglednike.

## API

| Resurs | Endpointi |
|---|---|
| Prijava | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, `PUT /api/auth/password` |
| Prvo pokretanje | `GET /api/setup`, `POST /api/setup/blank` |
| Postavke instance | `/api/settings`, `/api/settings/logo` |
| Prijevoznici | `/api/agencies` |
| Stanice | `/api/stops`, `/api/stops/area`, `/api/stops/{id}/routes`, `/api/stops/{id}/departures?date=` |
| Linije | `/api/routes`, `/api/routes/{id}/patterns`, `/api/routes/{id}/timetable?date=&directionId=` |
| Kalendari i iznimke | `/api/calendars`, `/api/calendars/{id}/dates` |
| Vožnje i vremena | `/api/trips`, `/api/trips/{id}/stop-times` |
| Trase | `/api/shapes` |
| Cijene karata | `/api/fares` |
| Izdavač feeda | `/api/feed-info` |
| GTFS izvoz | `GET /api/gtfs` |

Javno, bez prijave:

| Resurs | Endpointi |
|---|---|
| Sljedeći polasci sa stanice | `GET /api/public/departures/{stopCode}?limit=10` |
| Naziv, boje i logo | `GET /api/public/settings`, `GET /api/public/settings/logo` |
| GTFS feed (ako je uključen u postavkama) | `GET /api/public/gtfs` |
| Stanje aplikacije i baze | `GET /health` |

Greške se vraćaju kao [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457)
(`400` s greškama po polju, `404`, `409`, `422`).

## Dokumentacija

| Dokument | Sadržaj |
|---|---|
| [Roadmap](docs/roadmap.md) | faze razvoja, odluke i otvorena pitanja |
| [Lokalni razvoj](docs/development.md) | postavljanje, pokretanje, testovi, migracije |
| [Arhitektura](docs/architecture.md) | slojevi i tok zahtjeva |
| [Model podataka i GTFS](docs/data-model.md) | tablice, podržana GTFS polja, pravila |
| [GTFS izvoz](docs/gtfs-export.md) | sadržaj feeda, format i provjere |
| [Polasci, zasloni i PDF](docs/departures-and-timetables.md) | izračun polazaka, javna stranica stanice, PDF (plan) |
| [Shema baze](docs/terminus_schema.sql) | SQL shema s komentarima |
