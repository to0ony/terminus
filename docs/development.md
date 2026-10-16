# Lokalni razvoj

## Preduvjeti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 13 ili noviji
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- [Node.js](https://nodejs.org) 22 ili noviji (za frontend)

Naredbe za backend pokreću se iz mape `backend/` (`cd backend`), gdje su
`Terminus.slnx` i `global.json`; za frontend vidi [Frontend](#frontend).

## Baza i konfiguracija

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

GTFS uvoz prima ZIP do 200 MB. IIS Express ima vlastito ograničenje od 30 MB,
pa veće feedove treba slati na profil `http` ili `https` (Kestrel).

```sh
dotnet user-secrets set "Admin:Email" "admin@example.com" --project Terminus.WebApi
dotnet user-secrets set "Admin:Password" "<barem 10 znakova>" --project Terminus.WebApi
```

Stvaranje sheme u praznoj bazi:

```sh
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi
```

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

Dashboard i javni zaslon stanice su jedna aplikacija u mapi `frontend/`:
React, TypeScript, Vite, Mantine, React Router, TanStack Query, MapLibre GL.
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
| `npm run lint` | oxlint |
| `npm run format` / `format:check` | Prettier |
| `npm test` | Vitest i Testing Library (jsdom) |
| `npm run api:generate` | TypeScript tipovi API-ja iz OpenAPI opisa |

**API tipovi.** `src/api/schema.d.ts` generira se iz `/openapi/v1.json`
pokrenutog backenda i commita se, pa build ne treba backend. Nakon promjene
API-ja pokreće se `npm run api:generate`; `src/api/client.ts` (`openapi-fetch`)
tada odmah javlja neusklađene pozive kao greške tipova.

```
frontend/src/
  api/          generirani tipovi, klijent, greške (ApiError), QueryClient
  app/          providers, router, layout s izbornikom, 404 i greške rute
  components/   zajedničke komponente (učitavanje, greška upita, logo)
  features/     po području: auth, setup, dashboard, routes, stops, calendars,
                agencies, gtfs, settings, board
  test/         postavke testova i render s providerima
```

**Rute.** `/login` i javni `/board/:stopCode` su bez prijave. Ostalo je iza
`RequireAuth` (bez prijave vodi na `/login` i nakon prijave vraća na traženu
stranicu), a dashboard i iza `RequireSetup` (dok čarobnjak nije završen, vodi
na `/setup`). Stranice se učitavaju lazy. Kad API vrati 401 (istekla prijava),
klijent postavlja trenutnog korisnika na `null`, pa aplikacija prelazi na prijavu.

**Pozivi API-ja** idu kroz TanStack Query: `queryFn: async () => unwrap(await
api.GET(...))`. `unwrap` za neuspješan odgovor baca `ApiError` s porukom iz
ProblemDetails (`detail`, pa `title`), a greške po poljima (400) su u
`fieldErrors`. Greške upita i mutacija prikazuju se kao obavijest; forma koja
ih prikazuje sama postavlja `meta: { handlesErrors: true }`.

GitHub Actions za `frontend/` radi `npm ci`, lint, provjeru formata, testove i
build.
