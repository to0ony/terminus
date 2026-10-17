# Lokalni razvoj

## Preduvjeti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 13 ili noviji
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- [Node.js](https://nodejs.org) 22 ili noviji (za frontend)

Naredbe za backend pokreću se iz mape `backend/` (`cd backend`), gdje su
`Terminus.slnx` i `global.json`; za frontend vidi [Frontend](#frontend).

## Postavljanje baze i administratora

Aplikacija čita connection string `ConnectionStrings:Default`. U razvoju se
drži u [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets),
izvan repozitorija:

```sh
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=terminus;Username=<korisnik>;Password=<lozinka>" \
  --project Terminus.WebApi
```

Bez connection stringa aplikacija se ne pokreće.

Bez administratora se nitko ne može prijaviti, pa lokalno treba postaviti i
njega (stvara se pri pokretanju ako ne postoji). Prijevoznika ne treba
postavljati: stvara ga čarobnjak prvog pokretanja (`POST /api/setup/blank` ili uvoz
GTFS-a).

```sh
dotnet user-secrets set "Admin:Email" "admin@example.com" --project Terminus.WebApi
dotnet user-secrets set "Admin:Password" "<barem 10 znakova>" --project Terminus.WebApi
```

Stvaranje sheme u praznoj bazi:

```sh
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi
```

## Konfiguracija

Ključevi se zadaju kroz User Secrets, `appsettings` ili varijable okruženja
(`Admin__Email`, `ConnectionStrings__Default`, `PublicApi__PermitLimit` …).

| Ključ | Obavezno | Opis |
|---|---|---|
| `ConnectionStrings:Default` | da | Connection string za PostgreSQL. |
| `Admin:Email` | za seed | E-mail (korisničko ime) administratora. |
| `Admin:Password` | za seed | Početna lozinka administratora, barem 10 znakova. |
| `PublicApi:PermitLimit` | ne | Zahtjeva na javne endpointe u minuti po IP adresi (zadano 120). |
| `PublicApi:FeedPermitLimit` | ne | Preuzimanja javnog GTFS feeda u minuti po IP adresi (zadano 5). |
| `PublicApi:DeparturesCacheSeconds` | ne | Koliko se čuva popis polazaka stanice (zadano 20). |

**Administrator pri pokretanju.** Ako administrator s e-mailom `Admin:Email`
ne postoji, stvara se s lozinkom `Admin:Password`. Postojeći administratori se
nikad ne mijenjaju: promjena `Admin:Password` ne mijenja lozinku, a novi
administrator se dodaje promjenom `Admin:Email` i ponovnim pokretanjem.
Nepotpuna konfiguracija samo se zapisuje u log; aplikacija se normalno pokreće.

## Prvo pokretanje

Konfiguracija daje samo administratora. Nakon prve prijave dashboard prikazuje
čarobnjak (`GET /api/setup` vraća `completed: false`) s dva izbora:

- **uvoz postojećeg GTFS-a** – prijevoznici, stanice, linije, trase i varijante
  linija dolaze iz feeda (kalendari i vožnje ne); prvo se prikazuje izvještaj
  provjere s odabirom varijanti, a podaci se upisuju tek nakon potvrde (vidi
  [GTFS uvoz](gtfs-import.md));
- **početak od nule** – stvara se prvi prijevoznik (naziv i web adresa; kod,
  zona i jezik imaju zadane vrijednosti).

Kad je čarobnjak završen, ne prikazuje se ponovno ni ako se podaci kasnije
obrišu. Postavke instance (naziv, logo, boje, javna adresa) uređuju se na
stranici Postavke.

## Pokretanje

```sh
dotnet run --project Terminus.WebApi --launch-profile http
```

API je dostupan na `http://localhost:5183`. U okruženju `Development` OpenAPI
opis je na `/openapi/v1.json`; može se uvesti u Bruno ili Postman.
Primjeri zahtjeva nalaze se u `backend/Terminus.WebApi/Terminus.WebApi.http`;
prvi je prijava, a klijent (VS Code REST Client, Visual Studio) kolačić šalje
uz sljedeće zahtjeve. Za Bruno ili Postman isto vrijedi: prvo
`POST /api/auth/login`.
U Visual Studiju se otvara `backend/Terminus.slnx`.

GTFS uvoz prima ZIP do 200 MB. IIS Express ima vlastito ograničenje od 30 MB,
pa veće feedove treba slati na profil `http` ili `https` (Kestrel).

## Testovi

```sh
dotnet test --solution Terminus.slnx
```

Testovi su u `Terminus.Tests` (xUnit v3, Microsoft Testing Platform; način
pokretanja određuje `global.json`). Unit testovi ne trebaju bazu: servisi
dobivaju lažne repozitorije iz `Terminus.Tests/Fakes`.

GitHub Actions (`.github/workflows/ci.yml`) na svaki push i pull request na
`master` u mapi `backend/` radi restore, build u Release konfiguraciji i
pokreće testove.

## Migracije

Shema se mijenja isključivo kroz migracije:

```sh
# nova migracija nakon izmjene entiteta ili konfiguracije
dotnet ef migrations add <NazivMigracije> --project Terminus.DAL --startup-project Terminus.WebApi

# primjena na lokalnu bazu
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi

# idempotentna SQL skripta za ostala okruženja
dotnet ef migrations script --idempotent -o deploy.sql --project Terminus.DAL --startup-project Terminus.WebApi
```

Prije commita treba pregledati generiranu migraciju. EF ponekad doda
vrijednosti koje nisu poželjne, npr. `defaultValue: ""` kod pretvaranja
stupca u `NOT NULL`. Nakon promjene sheme ažurira se i
[`terminus_schema.sql`](terminus_schema.sql).

## Frontend

Naredbe se pokreću iz `frontend/`:

```sh
npm install
npm run dev            # http://localhost:5173
```

Vite proxy prosljeđuje `/api`, `/health` i `/openapi` na backend, pa su
frontend i API na istom originu: kolačić za prijavu radi, a CORS nije potreban.
Adresa backenda zadaje se u `frontend/.env.local` (predložak `.env.example`);
zadano je `http://localhost:5183`. Za IIS Express:

```sh
TERMINUS_API_URL=https://localhost:44376
```

| Naredba | Što radi |
|---|---|
| `npm run build` | provjera tipova i produkcijski build u `dist/` |
| `npm run typecheck` | samo provjera tipova |
| `npm run lint` | oxlint |
| `npm run format` / `format:check` | Prettier |
| `npm test` | Vitest i Testing Library (jsdom) |
| `npm run api:generate` | TypeScript tipovi API-ja iz OpenAPI opisa pokrenutog backenda |

Nakon promjene API-ja pokreće se `npm run api:generate` i commita
`src/api/schema.d.ts`. Struktura aplikacije, rute i pozivi API-ja opisani su u
[frontend.md](frontend.md).

GitHub Actions za `frontend/` radi `npm ci`, lint, provjeru formata, testove i
build.
