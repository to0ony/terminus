# Roadmap

Plan razvoja: smjer, faze i redoslijed. Gotovi koraci označeni su ✅, a
detaljan opis funkcija je u ostalim dokumentima (npr.
[polasci, zaslon i PDF](departures-and-timetables.md)).

## Stanje

| Faza | Stanje |
|---|---|
| 0 Osnova | ✅ |
| 1 Temelji (prijava, postavke, health check, čarobnjak) | ✅ |
| 2 Uvoz GTFS-a | ✅ |
| 3 Dashboard za pregled | ✅ osim trase linije na karti |
| 4 Za putnike (zaslon na stanici, PDF, javni feed) | ✅ osim PDF-a za sve stanice odjednom |
| 5 Editor | kalendari (sezone, tipovi dana, praznici) ✅, grafikon vožnji ✅, varijante linija ✅, uvoz kao osnova ✅, editor linija i varijanti na karti ✅, grafikon po varijantama ✅; sljedeće deploy pa trase |
| 6 Produkcija | čeka odluku o hostingu |

## Cilj

Prijevoznik dobiva vlastitu instancu Terminusa. Razvoj ide u dva velika koraka:

1. **Uvoz i objava.** Prijevoznik uveze postojeći GTFS feed, pregledava ga u
   dashboardu (stanice, linije, vozni redovi), za svaku stanicu preuzima PDF
   vozni red, a svaka stanica ima javnu stranicu s polascima uživo prema
   voznom redu.
2. **Uređivanje.** Editor za izmjene uvezenih podataka i izradu voznog reda
   od nule.

Prvi korak (faze 1–4) je dovoljan za pilot s prijevoznikom koji već ima GTFS;
za pilot još treba produkcija (faza 6). API za ručni unos (CRUD) već postoji i
čeka editor iz faze 5.

## Faza 0: osnova ✅

| Korak | Stanje |
|---|---|
| API za prijevoznike, stanice, linije, kalendare, vožnje i vremena | ✅ |
| GTFS izvoz s provjerama (`GET /api/gtfs`) | ✅ |
| Single-tenant: jedna tvrtka po instanci | ✅ |
| Unit testovi i CI | ✅ |
| Izračun polazaka (kalendari, ponoć, ljetno vrijeme, kolodvori) | ✅ |
| Javni endpoint polazaka `GET /api/public/departures/{stopCode}` | ✅ |

## Faza 1: temelji ✅

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

## Faza 2: uvoz GTFS-a ✅

| # | Korak | Ovisi o |
|---|---|---|
| 2.0 | Proširen GTFS model: sva polja osnovnih datoteka, `location_type` 0–4, prošireni `route_type`, trase (`shapes`), cijene (Fares v1), `feed_info`; API i izvoz za sve ✅ | — |
| 2.1 | `agencies.code` (GTFS `agency_id`), da se ID-evi prijevoznika sačuvaju pri uvozu i ponovnom izvozu ✅ | — |
| 2.2 | Uvoz GTFS ZIP-a: čitanje, provjera, prijevod GTFS ID-eva u `code`, zamjena podataka instance u jednoj transakciji; pregled pa potvrda; završava čarobnjak prvog pokretanja ✅ | 1.1, 1.4, 2.1 |
| 2.3 | Izvještaj o uvozu: broj uvezenih zapisa, greške koje zaustavljaju uvoz, upozorenja i preskočene datoteke ✅ | 2.2 |
| 2.4 | Uvoz `shapes.txt` i cijena (Fares v1); `frequencies.txt` se pretvara u pojedinačne vožnje ✅ | 2.2 |

Od 18. 10. 2026. uvoz donosi samo osnovu (prijevoznici, stanice, linije, trase,
varijante linija) i dopunjava postojeće podatke; kalendari i vožnje rade se u
aplikaciji (vidi plan editora u fazi 5). Detaljno u [gtfs-import.md](gtfs-import.md).

Datoteke koje Terminus ne podržava (npr. `transfers.txt`, Fares v2) navode se
u izvještaju kao preskočene.

## Faza 3: dashboard za pregled

| # | Korak | Ovisi o |
|---|---|---|
| 3.1 | Kostur aplikacije u `frontend/`: routing, prijava, API klijent generiran iz OpenAPI-ja ✅ | 1.1 |
| 3.2 | Zaslon za uvoz GTFS-a s izvještajem (u čarobnjaku prvog pokretanja) i stranica GTFS uvoza i izvoza s preuzimanjem izvoza ✅ | 2.3, 3.1 |
| 3.3 | Stanice: popis, pretraga, karta, detalj stanice s linijama i polascima ✅ | 3.1, 3.6 |
| 3.4 | Linije: popis s pretragom i filterima, detalj sa stanicama na liniji ✅; trasa na karti | 3.1, 3.6 |
| 3.5 | Vozni red linije: tablica vožnje × stanice za odabrani dan i smjer ✅ | 3.4, 3.6 |
| 3.6 | Backend za pregled: stanice na liniji, vozni red linije (matrica), polasci sa stanice za datum, linije stanice, sažetak vožnje u popisu, stanice unutar područja karte ✅ | — |
| 3.7 | Kalendari s iznimkama i prijevoznici (samo pregled) ✅ | 3.1 |
| 3.8 | Pregled: kalendari koji danas voze, do kada vrijedi vozni red; postavke instance i promjena lozinke ✅ | 3.1 |

Dashboard je u ovoj fazi samo za pregled. Uređivanje dolazi u fazi 5.

## Faza 4: za putnike

Detaljno u [departures-and-timetables.md](departures-and-timetables.md).

| # | Korak | Ovisi o |
|---|---|---|
| 4.1 | Output caching i rate limiting za `/api/public/*` ✅ | — |
| 4.2 | Javna stranica stanice `/board/{stopCode}`: sat i sljedeći polasci prema voznom redu ✅ | 1.2, 3.1, 4.1 |
| 4.3 | PDF vozni red za jednu stanicu (A4/A3, hrvatski/engleski, odabrano razdoblje), QR kod na stranicu stanice, preuzimanje iz dashboarda ✅; PDF za sve stanice odjednom | 3.3, 3.6, 4.2 |
| 4.4 | Javni GTFS feed `GET /api/public/gtfs` (za Google Maps i druge), uključuje se u postavkama, s cacheom iz 4.1 ✅ | 4.1 |

## Faza 5: editor

| # | Korak | Ovisi o |
|---|---|---|
| 5.1 | Odluka: vožnje ostaju u GTFS obliku i uređuju se kroz **grafikon voznog reda**; **varijante linije** (redoslijed stanica smjera s vremenima vožnje) su pravi zapis i predložak za nove vožnje ✅ | — |
| 5.2 | Uređivanje stanica (karta), prijevoznika i linija; kalendari kroz sezone voznog reda (razdoblje, tipovi dana, iznimke klikom na dan, i za praznu instancu) ✅ | 3.x |
| 5.3 | Uređivanje vožnji i vremena u grafikonu (povlačenje vožnje i točkica, točna vremena u panelu, tip dana, poništavanje, spremanje u jednoj transakciji s provjerom sukoba) ✅ | 5.1, 3.5 |
| 5.4 | Brzi unos: nova vožnja klikom, kopiranje s pomakom (npr. +30 min, 10 puta), generator takta, pomak i brisanje više vožnji ✅ | 5.3 |
| 5.5 | Praznici: državni praznici s OpenHolidays API-ja i jedno pravilo za sve tipove dana sezone ✅ | 5.2 |
| 5.6 | Trase (`shapes`): automatski prijedlog po cestama i prugama, ispravljanje povlačenjem; traži ruter (Valhalla ili pfaedle) | 2.4, 5.2, 6.1 |
| 5.7 | Izrada voznog reda od nule (prazna instanca bez uvoza) | 5.2–5.4 |
| 5.8 | Upravljanje administratorima: dodavanje, uklanjanje, reset lozinke | 1.1 |
| 5.9 | Upozorenja kod izvoza koja ne blokiraju izvoz (npr. feed istječe za 7 dana) | — |

**Plan editora (dogovoreno 18. 10. 2026.)** Aplikacija je alat u kojem se vozni
red radi, a GTFS uvoz je samo pomoć na početku. Ciljani su mali i srednji
prijevoznici. Redoslijed:

1. Varijante linija u backendu (prepoznavanje iz vožnji, API) ✅
2. **Uvoz kao osnova:** prijevoznici, stanice, linije, varijante s vremenima
   vožnje i trase, bez kalendara i vožnji; pregled varijanti pri uvozu
   (rijetke zadano isključene); ponovni uvoz dopunjava i ne dira sezone ni
   vožnje; čarobnjak vodi dalje (sezona, vožnje) ✅
3. **Editor linija i varijanti:** nova linija, varijanta klikom na stanice na
   karti, vremena vožnje po dionici, nova stanica i premještanje na karti ✅
4. **Grafikon po varijantama:** redci iz odabranih varijanti, takt i nova
   vožnja po varijanti, "Nacrtaj vožnju" (dva klika: prva i zadnja stanica,
   međustanice po vremenima vožnje; po želji stanica po stanica) ✅
5. Deploy (faza 6), pa trase za nove varijante (5.6).

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

## Tehničke odluke

| Tema | Odluka | Razlog |
|---|---|---|
| Uvoz | Samo osnova (stanice, linije, trase, varijante), kao dopuna | Tuđi kalendari i vožnje su često neuredni; vozni red se radi u aplikaciji, a ponovni uvoz ne smije pregaziti taj rad. |
| Autentikacija | ASP.NET Core Identity, HttpOnly cookie | Ugrađeno u .NET, korisnici u našoj bazi, bez dodatne infrastrukture. Frontend i API su na istoj domeni, pa cookie radi bez tokena u pregledniku. |
| Endpointi za prijavu | Vlastiti (`/api/auth/login`, `logout`, `me`) preko `SignInManager` | `MapIdentityApi` otvara javnu registraciju, a ovdje administratore dodaje samo drugi administrator. |
| CORS | Nije potreban | U razvoju Vite proxy šalje `/api` na backend; u produkciji je isti origin. |
| Frontend | React, TypeScript, Vite, React Router, TanStack Query, `openapi-typescript` + `openapi-fetch` | Tipovi iz OpenAPI opisa, cache i stanja učitavanja bez ručnog koda. |
| UI | Mantine; zaslon na stanici bez Mantinea (vlastiti CSS) | Gotove tablice, forme, upload, obavijesti i datumi za dashboard; zaslon ostaje lagan. |
| Karta | MapLibre GL s podlogom OpenFreeMap | Besplatno, bez API ključa i brzo s tisućama stanica. |
| PDF | QuestPDF i QRCoder | Vidi [departures-and-timetables.md](departures-and-timetables.md#35-licenca-za-questpdf) zbog licence. |

## Otvorena pitanja

- **Hosting**: gdje će instance raditi (VPS s Dockerom, Azure, vlastiti server)?
  O tome ovisi faza 6.
- **Licenca za QuestPDF** u produkciji: Community licenca ne vrijedi za javni
  sektor; vidi [licencu](departures-and-timetables.md#35-licenca-za-questpdf).
