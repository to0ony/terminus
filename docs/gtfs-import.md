# GTFS uvoz

```
POST /api/gtfs/import/preview   pregled: provjera i izvještaj, bez upisa
POST /api/gtfs/import           uvoz
```

Oba primaju GTFS ZIP kao `multipart/form-data` (polje `file`, najviše 200 MB)
i vraćaju isti izvještaj. Dashboard prvo šalje pregled, prikaže izvještaj i
nakon potvrde šalje istu datoteku na uvoz.

Uvoz **zamjenjuje sve podatke voznog reda** instance (prijevoznici, stanice,
linije, kalendari, vožnje, trase, cijene, `feed_info`) u jednoj transakciji.
Postavke instance i administratori ostaju. Ako feed ima greške, uvoz vraća
`422` s izvještajem i ništa ne mijenja. Uspješan uvoz završava čarobnjak prvog
pokretanja.

## Izvještaj

```json
{
  "imported": false,
  "canImport": true,
  "counts": { "agencies": 1, "stops": 3800, "routes": 154, "calendars": 7, "calendarDates": 95,
              "trips": 66661, "stopTimes": 1131537, "shapes": 561, "shapePoints": 82147,
              "fares": 0, "fareRules": 0, "feedInfo": true,
              "tripsFromFrequencies": 0, "interpolatedStopTimes": 0 },
  "existing": { "agencies": 0, "stops": 0, "routes": 0, "trips": 0 },
  "errors": [],
  "warnings": [
    { "file": "shapes.txt", "message": "Trasa s dvostrukim rednim brojem točke je preskočena.",
      "count": 9, "examples": ["shape_id '3_3'", "shape_id '161_1'"] }
  ],
  "skippedFiles": [ { "file": "transfers.txt", "reason": "Presjedanja nisu podržana." } ]
}
```

- `counts` – što se uvozi, nakon preskakanja neispravnih zapisa.
- `existing` – podaci koje uvoz briše (za upozorenje prije potvrde).
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
| nedostaje `agency.txt`, `stops.txt`, `routes.txt`, `trips.txt`, `stop_times.txt` ili oba `calendar.txt` i `calendar_dates.txt` |
| obavezni stupac nedostaje u obaveznoj datoteci ili u `frequencies.txt` |
| dvostruki `agency_id`, `stop_id`, `route_id`, `service_id`, `trip_id` ili `fare_id` |
| prijevoznik bez naziva, s neispravnom web adresom ili vremenskom zonom; prijevoznici s različitim zonama |
| nakon provjere nije ostala nijedna vožnja |

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

**Servisi samo u `calendar_dates.txt`** dobivaju kalendar bez dana u tjednu, s
razdobljem od prvog do zadnjeg datuma; datumi su iznimke.

**Nazivi kalendara** (za prikaz i stupce PDF voznog reda) izvode se iz dana u
tjednu: „Radni dan”, „Subota”, „Nedjelja”, „Subota i nedjelja”, „Radni dan i
subota”, „Svaki dan” ili popis dana („Pon, Sri, Pet”). Kalendar bez dana u
tjednu imenuje se po danima dodanih datuma: dani s barem pola najčešćeg broja
datuma su njegovi dani, a rijetki ostali su praznici („Nedjelja i praznici”).
Kalendar s do tri datuma imenuje se datumima („Posebni datumi: 24.12., 31.12.”).

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
| Izvještaj | `ImportReportBuilder` |
| Pregled i uvoz | `GtfsImportService` |
| Upis | `GtfsImportRepository`: `TRUNCATE` i binarni `COPY` u jednoj transakciji |

Čitanje i provjera feeda s milijun vremena traju nekoliko sekundi; upis u bazu
nekoliko desetaka sekundi (provjera stranih ključeva i indeksi u bazi). Isti
feed nakon izvoza i ponovnog uvoza daje iste brojeve bez upozorenja; to
pokriva i test `ImportAsync_ExportedFeed_RoundTrips`.
