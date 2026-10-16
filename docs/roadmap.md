# Roadmap

> [!IMPORTANT]
> Ovo je plan razvoja: smjer, faze i redoslijed. Detaljan opis pojedinih
> funkcija je u zasebnim dokumentima (npr.
> [polasci, zasloni i PDF](departures-and-timetables.md)). Kad se korak
> završi, označava se ovdje ✅.

## Cilj

Prijevoznik dobiva vlastitu instancu Terminusa. Razvoj ide u dva velika koraka:

1. **Uvoz i objava.** Prijevoznik uveze postojeći GTFS feed, pregledava ga u
   dashboardu (stanice, linije, vozni redovi), za svaku stanicu preuzima PDF
   vozni red, a svaka stanica ima javnu stranicu s polascima uživo prema
   voznom redu.
2. **Uređivanje.** Editor za izmjene uvezenih podataka i izradu voznog reda
   od nule.

Prvi korak (faze 1–4) je dovoljan za pilot s prijevoznikom koji već ima GTFS.
API za ručni unos (CRUD) već postoji i čeka editor iz faze 5.

## Faza 0: osnova ✅

| Korak | Stanje |
|---|---|
| API za prijevoznike, stanice, linije, kalendare, vožnje i vremena | ✅ |
| GTFS izvoz s provjerama (`GET /api/gtfs`) | ✅ |
| Single-tenant: jedna tvrtka po instanci | ✅ |
| Unit testovi i CI | ✅ |
| Izračun polazaka (kalendari, ponoć, ljetno vrijeme, kolodvori) | ✅ |
| Javni endpoint polazaka `GET /api/public/departures/{stopCode}` | ✅ |

## Faza 1: temelji

| # | Korak | Ovisi o |
|---|---|---|
| 1.1 | Autentikacija: više administratora, prijava i odjava, promjena lozinke, prvi administrator iz konfiguracije ✅ | — |
| 1.2 | Postavke instance: tablica `settings` (naziv, boje, logo, javni URL, objava GTFS-a); podaci za `feed_info.txt` su u `/api/feed-info` ✅ | 1.1 |
| 1.3 | Health check `/health` (aplikacija i baza) ✅ | — |
| 1.4 | Čarobnjak prvog pokretanja: stanje (`GET /api/setup`) i početak od nule (`POST /api/setup/blank`) i uvoz GTFS-a ✅ | 1.1 |

Sve osim `/api/public/*`, prijave i `/health` traži prijavu. Upravljanje
administratorima kroz dashboard dolazi u fazi 5; do tada se dodatni
administrator dodaje promjenom `Admin:Email` (i `Admin:Password`) i ponovnim
pokretanjem.

## Faza 2: uvoz GTFS-a

| # | Korak | Ovisi o |
|---|---|---|
| 2.0 | Proširen GTFS model: sva polja osnovnih datoteka, `location_type` 0–4, prošireni `route_type`, trase (`shapes`), cijene (Fares v1), `feed_info`; API i izvoz za sve ✅ | — |
| 2.1 | `agencies.code` (GTFS `agency_id`), da se ID-evi prijevoznika sačuvaju pri uvozu i ponovnom izvozu ✅ | — |
| 2.2 | Uvoz GTFS ZIP-a: čitanje, provjera, prijevod GTFS ID-eva u `code`, zamjena podataka instance u jednoj transakciji; pregled pa potvrda; završava čarobnjak prvog pokretanja ✅ | 1.1, 1.4, 2.1 |
| 2.3 | Izvještaj o uvozu: broj uvezenih zapisa, greške koje zaustavljaju uvoz, upozorenja i preskočene datoteke ✅ | 2.2 |
| 2.4 | Uvoz `shapes.txt` i cijena (Fares v1); `frequencies.txt` se pretvara u pojedinačne vožnje ✅ | 2.2 |

Uvoz zamjenjuje sve podatke voznog reda (prijevoznici, stanice, linije,
kalendari, vožnje, trase, cijene). Novi vozni red za sezonu uvozi se istim
putem. Spajanje s postojećim podacima nije potrebno dok ne postoji editor.
Detaljno u [gtfs-import.md](gtfs-import.md).

Datoteke koje Terminus ne podržava (npr. `transfers.txt`, Fares v2) navode se
u izvještaju kao preskočene. Ponovni izvoz ih neće sadržavati. Provjereno na
feedu ZET-a (1,1 milijun vremena): izvoz i ponovni uvoz daju iste podatke.

Uvoz se testira krugom uvoz → izvoz → uvoz, uz stvarne feedove i
[MobilityData validator](https://gtfs-validator.mobilitydata.org/).

## Faza 3: dashboard za pregled

| # | Korak | Ovisi o |
|---|---|---|
| 3.1 | Kostur aplikacije u `frontend/`: routing, prijava, API klijent generiran iz OpenAPI-ja ✅ | 1.1 |
| 3.2 | Zaslon za uvoz GTFS-a s izvještajem (u čarobnjaku prvog pokretanja ✅); stranica GTFS uvoza i izvoza s preuzimanjem izvoza | 2.3, 3.1 |
| 3.3 | Stanice: popis, pretraga, karta, detalj stanice s linijama i polascima | 3.1, 3.6 |
| 3.4 | Linije: popis, detalj s trasom na karti i stanicama na liniji | 3.1, 3.6 |
| 3.5 | Vozni red linije: tablica vožnje × stanice za odabrani dan ili kalendar | 3.4, 3.6 |
| 3.6 | Backend za pregled: stanice na liniji, vozni red linije (matrica), polasci sa stanice za datum, linije stanice, sažetak vožnje u popisu, stanice unutar područja karte ✅ | — |
| 3.7 | Kalendari s iznimkama i prijevoznici (samo pregled) | 3.1 |

Dashboard je u ovoj fazi samo za pregled. Uređivanje dolazi u fazi 5.

## Faza 4: za putnike

Detaljno u [departures-and-timetables.md](departures-and-timetables.md).

| # | Korak | Ovisi o |
|---|---|---|
| 4.1 | Output caching i rate limiting za `/api/public/*` | — |
| 4.2 | Javna stranica stanice `/board/{stopCode}`: sat i sljedeći polasci prema voznom redu | 1.2, 3.1, 4.1 |
| 4.3 | PDF vozni red za jednu stanicu i za sve stanice, QR kod na stranicu stanice, preuzimanje iz dashboarda | 3.3, 3.6, 4.2 |
| 4.4 | Javni GTFS feed `GET /api/public/gtfs` (za Google Maps i druge), uključuje se u postavkama ✅; caching dolazi s 4.1 | 4.1 |

## Faza 5: editor

| # | Korak | Ovisi o |
|---|---|---|
| 5.1 | Odluka: vožnje kao danas (svaka sa svojim stanicama) ili obrasci vožnje (redoslijed stanica i vremena vožnje + polazno vrijeme) | — |
| 5.2 | Uređivanje stanica (karta), prijevoznika, linija i kalendara | 3.x |
| 5.3 | Uređivanje vožnji i vremena | 5.1, 3.5 |
| 5.4 | Brzi unos: kopiranje vožnje s pomakom (npr. +30 min, 10 puta), pomicanje vremena, grupno brisanje | 5.3 |
| 5.5 | Praznici: jedan popis datuma koji se primjenjuje na više kalendara | 5.2 |
| 5.6 | Uređivanje trasa (`shapes`) | 2.4, 5.2 |
| 5.7 | Izrada voznog reda od nule (prazna instanca bez uvoza) | 5.2–5.4 |
| 5.8 | Upravljanje administratorima: dodavanje, uklanjanje, reset lozinke | 1.1 |
| 5.9 | Upozorenja kod izvoza koja ne blokiraju izvoz (npr. feed istječe za 7 dana) | — |

Odluka 5.1 donosi se prije dizajna zaslona za vožnje. Ako se uvedu obrasci,
uvoz (faza 2) treba prepoznati obrasce u uvezenim vožnjama.

## Faza 6: produkcija

| # | Korak | Ovisi o |
|---|---|---|
| 6.1 | Odluka o hostingu (vidi otvorena pitanja) | — |
| 6.2 | Docker image za API i frontend | — |
| 6.3 | Ponovljiv deploy: nova instanca = konfiguracija + jedna naredba; HTTPS preko reverse proxyja | 6.1, 6.2 |
| 6.4 | Primjena migracija na produkciji | 6.3 |
| 6.5 | Backup baze i provjereni postupak vraćanja | 6.3 |
| 6.6 | Praćenje: `/health` i obavijest kad instanca ili baza padne | 1.3, 6.3 |

Faza 6 ne ovisi o fazama 2–5 i može se raditi usporedno; mora biti gotova
prije prvog pilota.

## Kasnije

- Audit log: tko je i kada promijenio koji zapis.
- Zaboravljena lozinka preko e-maila (traži SMTP konfiguraciju).
- Cijene karata (`fare_*`) i ostale neobavezne GTFS datoteke.
- Uvoz iz Excel/CSV predloška, ako se pokaže potreba.
- GTFS-Realtime i praćenje vozila (stvarna kašnjenja na zaslonima).
- Uloge s različitim ovlastima (npr. samo pregled).

## Odluke

| Tema | Odluka |
|---|---|
| Redoslijed | Prvo uvoz GTFS-a, dashboard za pregled, PDF i javne stranice stanica; editor i izrada od nule nakon toga. |
| Prvi administrator | Stvara se pri pokretanju iz konfiguracije (`Admin:Email`, `Admin:Password` u User Secrets ili varijablama okruženja). Nema zaslona za stvaranje administratora: na javnoj instanci tko ga prvi otvori, postao bi administrator. |
| Prvi prijevoznik | Ne dolazi iz konfiguracije. Nakon prve prijave čarobnjak nudi uvoz postojećeg GTFS-a (prijevoznici iz `agency.txt`) ili početak od nule (unos prijevoznika). Uvoz prvo prikazuje izvještaj, a upisuje tek nakon potvrde. Postavke instance uređuju se naknadno, ne u čarobnjaku. |
| Zaboravljena lozinka | Za početak lozinku resetira drugi administrator. |

## Prijedlozi koje treba potvrditi

| Tema | Prijedlog | Razlog |
|---|---|---|
| Uvoz | Zamjenjuje sve podatke voznog reda | Bez editora nema ručnih izmjena koje bi se izgubile; nova sezona = novi uvoz. |
| Autentikacija | ASP.NET Core Identity, HttpOnly cookie | Ugrađeno u .NET, korisnici u našoj bazi, bez dodatne infrastrukture. Frontend i API su na istoj domeni, pa cookie radi bez tokena u pregledniku. |
| Endpointi za prijavu | Vlastiti (`/api/auth/login`, `logout`, `me`) preko `SignInManager` | `MapIdentityApi` otvara javnu registraciju, a ovdje administratore dodaje samo drugi administrator. |
| CORS | Nije potreban | U razvoju Vite proxy šalje `/api` na backend; u produkciji je isti origin. |
| Frontend | React, TypeScript, Vite, React Router, TanStack Query, `openapi-typescript` + `openapi-fetch` | Tipovi iz OpenAPI opisa, cache i stanja učitavanja bez ručnog koda. |
| UI i karta | Mantine; MapLibre GL s OpenStreetMap podlogom | Mantine ima gotove tablice, forme, upload, obavijesti i datume za dashboard. MapLibre je besplatan, bez API ključa, i brz s tisućama stanica. Zaslon na stanici ne koristi Mantine (lagan CSS). |
| UI biblioteka | Mantine | Bogate forme i tablice, brz razvoj dashboarda. Alternativa: shadcn/ui. |
| Karta | Leaflet s OpenStreetMapom | Besplatno; prikaz stanica i trasa, kasnije i odabir koordinata. |
| PDF | QuestPDF i QRCoder | Vidi [departures-and-timetables.md](departures-and-timetables.md#34-biblioteke). |

## Otvorena pitanja

- **Hosting**: gdje će instance raditi (VPS s Dockerom, Azure, vlastiti server)?
  O tome ovisi faza 6.
- **Dolasci na javnoj stranici stanice**: samo polasci ili i dolasci (npr. na
  kolodvoru gdje vožnje završavaju)?
- Pitanja o PDF-u: vidi
  [departures-and-timetables.md](departures-and-timetables.md#6-otvorena-pitanja).
