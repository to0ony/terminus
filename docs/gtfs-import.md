# GTFS uvoz

```
POST /api/gtfs/import/preview   pregled: provjera, izvještaj i varijante, bez upisa
POST /api/gtfs/import           uvoz (polje "variants": odabrane varijante)
```

Oba primaju GTFS ZIP kao `multipart/form-data` (polje `file`, najviše 200 MB)
i vraćaju isti izvještaj. Dashboard prvo šalje pregled, prikaže izvještaj i
odabir varijanti, a nakon potvrde šalje istu datoteku s poljima `variants` (JSON
popis ključeva iz pregleda; bez njega se uvoze sve) i `skipExpired` (izostavlja
kalendare koji više ne voze).

**Uvoz zamjenjuje sve podatke voznog reda.** U jednoj transakciji briše se sve,
uključujući sezone i vožnje rađene u aplikaciji, i upisuje feed. Ostaju samo
korisnici i postavke instance. Ako upis ne uspije, podaci ostaju netaknuti, a
feed s greškama vraća `422` s izvještajem i ne mijenja ništa. Zato je uvoz
namijenjen početku rada ili prelasku na novi feed, a ne redovitom osvježavanju.
Dashboard upozorava već pri odabiru datoteke, a prije uvoza traži potvrdu s
brojem zapisa koji se brišu. Uspješan uvoz završava čarobnjak prvog pokretanja.

| Uvozi se | Napomena |
|---|---|
| prijevoznici, stanice (s kolodvorima i peronima), linije | |
| trase (`shapes.txt`) | |
| **varijante linija** | prepoznate iz vožnji, s vremenima vožnje (vidi niže) |
| kalendari i iznimke | izvan sezona, kakvi su u feedu |
| vožnje i vremena | povezane s varijantom po kojoj voze |
| cijene (Fares v1) i `feed_info` | |

## Istekli kalendari

Kalendar je istekao ako od današnjeg dana (u zoni prijevoznika) nema nijedan dan
vožnje. Računa se iz stvarnih dana, a ne iz `end_date`: feed može imati kalendar
do 2030. bez ijednog dana u tjednu, sa svim datumima u `calendar_dates.txt`.
Izvještaj u `expired` navodi koliko je kalendara, vožnji i vremena isteklo. Uz
`skipExpired` oni se ne uvoze, a dashboard je tu opciju zadano uključio.

## Varijante linija

Vožnje iste linije, istog smjera i istog redoslijeda stanica čine varijantu
(npr. glavna, skraćena, iz garaže). Vrijeme vožnje između stanica i stajanje
su medijan njezinih vožnji, a natpis smjera i trasa najčešći. Naziv je
„prva → zadnja stanica” (isti naziv dobiva broj: „… (2)”). Unutar smjera
varijanta s najviše vožnji je glavna.

Zadano se uvoze sve varijante. **Vožnje neodabrane varijante se ne uvoze**, pa
bi izostavljanje rijetkih varijanti izgubilo stvarne polaske. Ključ varijante je
sažetak linije, smjera i GTFS ID-eva stanica, pa isti feed daje iste ključeve i
u pregledu i u uvozu. Varijanta sa samo isteklim vožnjama ima
`expiredTripCount = tripCount` i ne uvozi se uz `skipExpired`.

## Izvještaj

```json
{
  "imported": false,
  "canImport": true,
  "counts": { "agencies": 1, "stops": 3802, "routes": 154, "calendars": 10, "calendarDates": 72,
              "trips": 98915, "stopTimes": 1697138, "shapes": 536, "shapePoints": 78469,
              "fares": 0, "fareRules": 0, "feedInfo": true,
              "tripsFromFrequencies": 0, "interpolatedStopTimes": 0 },
  "existing": { "agencies": 0, "stops": 0, "routes": 0, "trips": 0, "seasons": 0 },
  "expired": { "calendars": 5, "trips": 47892, "stopTimes": 824980 },
  "variants": [
    { "key": "3f1c…", "routeCode": "6", "routeName": "6", "directionId": 0,
      "name": "Črnomerec → Sopot", "headsign": "Sopot", "stopCount": 23, "tripCount": 584, "expiredTripCount": 280,
      "duration": 2340, "share": 0.66, "main": true, "included": true }
  ],
  "errors": [],
  "warnings": [
    { "file": "shapes.txt", "message": "Trasa s dvostrukim rednim brojem točke je preskočena.",
      "count": 9, "examples": ["shape_id '3_3'", "shape_id '161_1'"] }
  ],
  "skippedFiles": [ { "file": "transfers.txt", "reason": "Presjedanja nisu podržana." } ]
}
```

- `counts` – što se uvozi, nakon preskakanja neispravnih zapisa i primjene
  odabira.
- `existing` – podaci u instanci prije uvoza; uvoz ih briše.
- `expired` – dio feeda koji od današnjeg dana više ne vozi.
- `variants` – prepoznate varijante linija; `included` kaže uvozi li se.
- `errors` – zbog njih se feed ne može uvesti (`canImport: false`).
- `warnings` – zapisi koji su preskočeni ili ispravljeni; uvoz je moguć.
- Isti problem iz više redaka je jedan unos s brojem (`count`) i do pet
  primjera s brojem retka i ID-em.

## Greške i upozorenja

Pravilo: što bi pokvarilo cijeli feed je **greška**; neispravan pojedinačni
zapis se **preskače ili ispravlja uz upozorenje**, a ono što o njemu ovisi
preskače se lančano (npr. vožnja na preskočenoj liniji).

| Greška (uvoz se odbija) |
|---|
| datoteka nije ZIP ili je raspakirana veća od 2 GB |
| nedostaje `agency.txt`, `stops.txt` ili `routes.txt` |
| obavezni stupac nedostaje u obaveznoj datoteci ili u `frequencies.txt` |
| dvostruki `agency_id`, `stop_id`, `route_id`, `service_id`, `trip_id` ili `fare_id` |
| prijevoznik bez naziva, s neispravnom web adresom ili vremenskom zonom; prijevoznici s različitim zonama |

Bez `trips.txt` i `stop_times.txt`, ili kad nakon provjere ne ostane nijedna
vožnja, uvoz je moguć, ali bez varijanti (upozorenje). Bez kalendara
(`calendar.txt` i `calendar_dates.txt`) vožnje se ne uvoze, nego služe samo za
varijante (upozorenje).

| Upozorenje | Posljedica |
|---|---|
| stanica bez naziva ili koordinata (`location_type` 0–2), nepoznat `location_type` | stanica preskočena |
| ulaz, čvor ili mjesto ukrcavanja bez odgovarajuće nadređene stanice | preskočen |
| linija s nepostojećim prijevoznikom ili nepoznatim `route_type` | linija preskočena |
| linija bez kratke oznake i naziva | oznaka je `route_id` |
| kalendar s neispravnim danima ili datumima | preskočen |
| vožnja na preskočenoj liniji ili kalendaru | preskočena |
| vožnja kroz nepostojeću stanicu ili stanicu koja nije stajalište/peron, GTFS-Flex | preskočena |
| vožnja s manje od dvije stanice, bez vremena na prvoj ili zadnjoj stanici, s vremenima unatrag ili iza 48:00:00 | preskočena |
| trasa s manje od dvije točke ili dvostrukim rednim brojem točke | trasa preskočena, vožnje bez trase |
| `shape_dist_traveled` koji se smanjuje | zanemaren za tu trasu ili vožnju |
| neispravna boja, adresa, e-mail, jezik, vremenska zona stanice, neobavezni enum | vrijednost zanemarena |
| cijena s neispravnom cijenom ili valutom; pravilo za nepostojeću cijenu ili liniju, bez linije i zona | preskočeno |
| neispravan `feed_info.txt` | nije uvezen; izvoz koristi prvog prijevoznika |

## Pretvorbe

**GTFS ID-evi** se spremaju u stupce `code`, pa ponovni izvoz piše iste ID-eve.
Bez `agency_id` (feed s jednim prijevoznikom) kod se izvodi iz naziva.

**Prazna vremena.** GTFS dopušta prazna vremena na stanicama koje nisu
timepoint; Terminus ih uvijek ima. Vremena između dvije poznate stanice
računaju se razmjerno udaljenosti: po `shape_dist_traveled` ako ga imaju sve
stanice u razmaku, inače po zračnoj udaljenosti stanica, a bez koordinata
jednoliko. Takva vremena imaju `timepoint = 0` (okvirno). Ako je zadano samo
vrijeme dolaska ili polaska, drugo je isto.

**`frequencies.txt`.** Vožnja zadana intervalom postaje niz pojedinačnih
vožnji: polasci u `start_time`, `start_time + headway_secs` … (prije
`end_time`), s istim razmacima između stanica kao predložak. Kod nove vožnje je
`trip_id` i vrijeme polaska, npr. `T1-072000`. Predložak se ne uvozi.

**Redni brojevi** (`stop_sequence`, `shape_pt_sequence`) se poredaju i
prenumeriraju u 1, 2, 3 …

## Nepodržano

Navode se u `skippedFiles` i ne uvoze se: `transfers.txt`, `pathways.txt`,
`levels.txt`, `translations.txt`, `attributions.txt`, Fares v2
(`fare_products.txt` …) i GTFS-Flex (`booking_rules.txt`, `locations.geojson`
…). Ignoriraju se i nepoznati stupci, npr. `stops.level_id` i `feed_info`
datumi i verzija (izvoz ih računa sam).

## Format

- UTF-8, s BOM-om ili bez; retci s CRLF, LF ili CR; navodnici po RFC 4180.
- Datoteke mogu biti u korijenu ZIP-a ili u jednoj podmapi.
- Vremena `H:MM:SS` ili `HH:MM:SS`, do 48:00:00; datumi `YYYYMMDD`.

## Implementacija

| Dio | Gdje |
|---|---|
| CSV i ZIP | `GtfsCsvReader`, `GtfsArchive` (`Terminus.Service/Gtfs/Import`) |
| Provjere i pretvorbe | `GtfsFeedReader` (stanice, linije, kalendari, trase, cijene, vožnje i vremena) |
| Istekli kalendari | `ExpiredCalendars` |
| Izvještaj | `ImportReportBuilder` |
| Varijante iz vožnji | `RouteVariantBuilder` |
| Pregled i uvoz | `GtfsImportService` |
| Upis | `GtfsImportRepository`: `TRUNCATE` i upis u jednoj transakciji; kalendari, točke trasa, vožnje i vremena binarnim `COPY`-jem |

ZET-ov feed (studeni 2026.) ima 3.802 stanice, 154 linije, 98.915 vožnji s 1,7
milijuna vremena i 532 varijante; 48 % vožnji je u kalendarima koji su već
istekli.
