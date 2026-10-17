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
| `/routes`, `/routes/:id` | linije (pretraga, filteri, nova linija); linija s izmjenom podataka, **varijantama** i voznim redom po danu i smjeru |
| `/routes/:id/variants/:variantId` | **editor varijante** na karti (`new`, `new?from=` za kopiju) (opis niže) |
| `/routes/:id/schedule` | **grafikon voznog reda** linije s uređivanjem vožnji (opis niže) |
| `/stops`, `/stops/:id` | stanice s kartom; stanica s linijama, polascima za dan, premještanjem na karti, **PDF voznim redom** i poveznicom na zaslon |
| `/calendars` | sezone voznog reda (i nova sezona) te ostali kalendari, npr. uvezeni iz GTFS-a |
| `/seasons/:id` | sezona: tipovi dana, pravilo za praznike i svi mjeseci; klik na dan mijenja kako vozi (kao drugi tip dana, ne vozi) uz napomenu |
| `/calendars/:id` | kalendar s danima u tjednu i iznimkama na mjesečnom prikazu |
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
                seasons, agencies, gtfs, settings, appearance, board
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

## Varijante linije

Varijanta je redoslijed stanica jednog smjera s vremenom vožnje od prethodne
stanice i stajanjem; po njoj nastaju vožnje u grafikonu. Na stranici linije su
sve varijante (glavna, iz GTFS-a, broj vožnji) s izmjenom, kopiranjem i
brisanjem (ne dok je koriste vožnje); linija bez varijanti ih može prepoznati
iz postojećih vožnji.

Editor varijante (`features/variants`):

- **Klik na stanicu** na karti dodaje je na kraj; vrijeme vožnje predlaže se iz
  zračne udaljenosti (uvećane za trećinu) i brzine vrste prijevoza,
  zaokruženo na pola minute (`variantMath.ts`). „Predloži vremena” računa sve
  dionice iznova.
- **Klik na prazno mjesto** karte otvara novu stanicu na tom mjestu.
- Stanice se pomiču gore i dolje i uklanjaju; varijanta je na karti
  numerirana crta. Naziv je zadano „prva → zadnja stanica”.
- Varijanta koju koriste vožnje zadržava stanice, a mijenjaju se samo vremena
  (za drugi redoslijed kopija).

## Grafikon voznog reda

`features/schedule` je editor vožnji jedne linije u obliku grafikona
(Mareyev dijagram, kao željeznički grafikon prometa):

- **Redci** su stanice smjera: spojeni redoslijed varijanti i vožnji, isti za
  sve tipove dana. Traka varijanti (s brojem vožnji) skriva ili prikazuje
  varijantu, a redci prate prikazane. Razmak između stanica je srednje vrijeme vožnje, pa je
  pravilna vožnja ravna crta, a sporiji dio se vidi kao lom.
- **Vodoravno je vrijeme** dana voznog reda, s ponoći označenom crtkano
  (vožnje iza nje su GTFS vremena 24:xx). Traka gore pokazuje gustoću polazaka
  cijelog dana i prozor koji se vidi; kotačić zumira, a Shift+kotačić pomiče.
- **Vožnja je crta kroz točkice polazaka**, u boji tipa dana. Odabrani tip
  dana se uređuje, a drugi se može prikazati blijedo za usporedbu.
- **Traka intervala** dolje pokazuje razmak između uzastopnih polazaka.

Uređivanje:

| Radnja | Što radi |
|---|---|
| Klik / Ctrl+klik / povlačenje po praznom | odabir vožnje, dodavanje u odabir, odabir okvirom |
| Povlačenje crte | pomak cijele vožnje (svih odabranih), na minutu |
| Povlačenje točkice | vrijeme na toj stanici; sa Shiftom i sve stanice iza nje |
| Klik u retku prve stanice | nova vožnja po varijanti odabranoj pod „Nove vožnje po” (linija bez varijanti kopira postojeću vožnju) |
| „Nacrtaj vožnju” | klik polaska na prvoj stanici, po želji vremena na sljedećim, kraj klikom na zadnjoj stanici ili Enterom; stanice između po vremenima vožnje varijante; Backspace briše zadnju točku, Esc odustaje |
| ← → (Shift: ±5) | pomak odabranih za minutu |
| Del, Ctrl+Z, Ctrl+Y | brisanje, poništavanje, ponavljanje |
| Panel desno | točna vremena (dolazak i polazak), natpis smjera, tip dana, kopiranje s pomakom (npr. svakih 10 min, 5 puta), generator takta (od–do svakih N min, po odabranoj varijanti) |

GTFS pravila (vremena ne padaju, polazak nije prije dolaska, 0–48 h) provjerava
se odmah: neispravna vožnja je crvena, a spremanje je onemogućeno. Izmjene se
spremaju zajedno, u jednoj transakciji (`PUT /api/routes/{id}/schedule`).
Vožnja koju je u međuvremenu promijenio netko drugi vraća 409 i nudi ponovno
učitavanje. Izlazak sa stranice s nespremljenim izmjenama traži potvrdu.

Stanje je u `useScheduleEditor` (povijest za poništavanje, odabir), a čista
logika (raspored redaka, pomaci, provjere, nove vožnje, razlika za spremanje) u
`model.ts` s testovima. Crtanje je na `canvas`u, da i linija sa stotinama
vožnji ostane glatka pri povlačenju.

## Zaslon na stanici

`features/board` namjerno ne koristi Mantine komponente nego vlastiti CSS
(`BoardPage.module.css`), da bude lagan na ekranima na stanicama. Ponašanje je
opisano u [departures-and-timetables.md](departures-and-timetables.md#23-izgled-i-ponašanje).
