# Frontend

Dashboard za administratora i javni zaslon stanice su jedna aplikacija u mapi
`frontend/`: React 19, TypeScript, Vite, Mantine, React Router, TanStack Query
i MapLibre GL. Pokretanje i naredbe su u [lokalnom razvoju](development.md#frontend).

## Stranice

| Ruta | Stranica |
|---|---|
| `/login` | prijava |
| `/setup` | čarobnjak prvog pokretanja: uvoz GTFS-a ili početak od nule |
| `/` | pregled: kalendari koji danas voze, do kada vrijedi vozni red, broj linija i stanica |
| `/routes`, `/routes/:id` | linije (pretraga, filteri po prijevozniku, vrsti i stanju); vozni red linije po danu i smjeru |
| `/stops`, `/stops/:id` | stanice s kartom; stanica s linijama, polascima za dan, **PDF voznim redom** i poveznicom na zaslon |
| `/calendars`, `/calendars/:id` | kalendari; kalendar s danima u tjednu i iznimkama na mjesečnom prikazu |
| `/agencies` | prijevoznici s brojem linija |
| `/gtfs` | GTFS izvoz (s popisom problema ako izvoz nije moguć) i ponovni uvoz |
| `/settings` | naziv, boje i logo javnih stranica, javna adresa, objava GTFS feeda, tema i podloga karte, promjena lozinke |
| `/board/:stopCode` | **javni zaslon stanice**, bez prijave ([opis](departures-and-timetables.md#2-zaslon-na-stanici)) |

Filteri, odabrani datum i stranica popisa čuvaju se u adresi (`?q=&date=&page=`),
pa se prikaz može podijeliti poveznicom i preživi osvježavanje.

## Struktura

```
frontend/src/
  api/          generirani tipovi (schema.d.ts), klijent, ApiError, QueryClient
  app/          providers, router, layout s izbornikom, 404 i greške rute
  components/   zajedničke komponente (učitavanje, greška upita, logo, karta)
  features/     po području: auth, setup, dashboard, routes, stops, calendars,
                agencies, gtfs, settings, appearance, board
  lib/          pomoćne funkcije (formati, boje, adrese, preuzimanje datoteka)
  test/         postavke testova i render s providerima
```

Svako područje u `features/` ima svoje stranice, komponente i `queries.ts`
s pozivima API-ja. Čista logika (npr. grupiranje polazaka po satu, nazivi
dana, kontrast boja) je u zasebnim datotekama s testovima uz njih.

## Rute i prijava

`/login` i `/board/:stopCode` su bez prijave. Ostalo je iza `RequireAuth`
(bez prijave vodi na `/login` i nakon prijave vraća na traženu stranicu), a
dashboard i iza `RequireSetup` (dok čarobnjak nije završen, vodi na `/setup`).
Stranice se učitavaju lazy, pa javni zaslon ne učitava kod dashboarda. Kad API
vrati 401 (istekla prijava), klijent postavlja trenutnog korisnika na `null`,
pa aplikacija prelazi na prijavu.

## Pozivi API-ja

Frontend i API su na istom originu (u razvoju preko Vite proxyja), pa kolačić
za prijavu radi bez CORS-a.

**Tipovi.** `src/api/schema.d.ts` generira se iz `/openapi/v1.json` pokrenutog
backenda (`npm run api:generate`) i commita se, pa build ne treba backend.
`src/api/client.ts` (`openapi-fetch`) nakon promjene API-ja odmah javlja
neusklađene pozive kao greške tipova.

**Upiti** idu kroz TanStack Query: `queryFn: async () => unwrap(await
api.GET(...))`. `unwrap` za neuspješan odgovor baca `ApiError` s porukom iz
ProblemDetails (`detail`, pa `title`), greškama po poljima (`fieldErrors`, 400)
i popisom problema (`problems`, 422). Greške upita i mutacija prikazuju se kao
obavijest; komponenta koja ih prikazuje sama postavlja
`meta: { handlesErrors: true }`.

**Datoteke** (GTFS izvoz, PDF) preuzimaju se kao blob (`parseAs: 'blob'`), a
naziv datoteke dolazi iz `Content-Disposition` (`lib/download.ts`).

## Tema i karta

Tema sučelja (prema sustavu, svijetla ili tamna) bira se u korisničkom
izborniku ili na stranici Postavke; Mantine je pamti u pregledniku.

`components/map/MapView` je MapLibre GL s podlogom
[OpenFreeMap](https://openfreemap.org) (bez API ključa). Podloga se bira gumbom
u donjem lijevom kutu karte ili u Postavkama: prema temi (zadano), Positron,
Dark, Liberty, Bright ili Fiord. Izbor se pamti u pregledniku
(`terminus-map-style`) i vrijedi za sve karte; pri promjeni podloge slojevi
stranice (npr. stanice) ostaju. Stanice se učitavaju za vidljivi dio karte (`/api/stops/area`,
najviše 2000), a početni prikaz je područje svih stanica (`/api/stops/extent`).

## Zaslon na stanici

`features/board` namjerno ne koristi Mantine komponente nego vlastiti CSS
(`BoardPage.module.css`), da bude lagan na ekranima na stanicama. Ponašanje je
opisano u [departures-and-timetables.md](departures-and-timetables.md#23-izgled-i-ponašanje).
