# Model podataka i GTFS

Model podataka prati [GTFS Schedule](https://gtfs.org/documentation/schedule/reference/)
(General Transit Feed Specification), uz nekoliko razlika koje olakšavaju
uređivanje podataka u aplikaciji.

## Tablice i GTFS datoteke

| Tablica | GTFS datoteka | GTFS ID | Napomena |
|---|---|---|---|
| `agencies` | `agency.txt` | `code` → `agency_id` | Prijevoznici (operateri linija). |
| `stops` | `stops.txt` | `code` → `stop_id` | Stajališta, peroni, stanice, ulazi, čvorovi i mjesta ukrcavanja (`location_type` 0–4). |
| `routes` | `routes.txt` | `code` → `route_id` | Linije. |
| `service_calendars` | `calendar.txt` | `code` → `service_id` | Tjedni obrazac vožnje u razdoblju. |
| `calendar_dates` | `calendar_dates.txt` | — | Iznimke kalendara po datumu. |
| `trips` | `trips.txt` | `code` → `trip_id` | Pojedinačne vožnje. |
| `stop_times` | `stop_times.txt` | — | Vremena vožnje po stanicama. |
| `shapes`, `shape_points` | `shapes.txt` | `code` → `shape_id` | Trase vožnji na karti. |
| `fares`, `fare_rules` | `fare_attributes.txt`, `fare_rules.txt` | `code` → `fare_id` | Cijene karata i pravila primjene (Fares v1). |
| `feed_info` | `feed_info.txt` | — | Izdavač feeda; najviše jedan redak. |

Tablice izvan GTFS-a:

| Tablica | Namjena |
|---|---|
| `settings` | Postavke instance (naziv, boje, logo, javni URL, objava GTFS-a); najviše jedan redak. |
| `admin_users`, `admin_user_claims`, `admin_user_logins`, `admin_user_tokens` | Administratori (ASP.NET Core Identity, bez uloga). Korisničko ime je e-mail. |
| `data_protection_keys` | Ključevi za šifriranje kolačića prijave (ASP.NET Core Data Protection). |

Veze:

- `agencies` 1 → N `routes` (prijevoznik koji vozi liniju), `agencies` 1 → N `fares`
- `stops` 1 → N `stops` (peroni, ulazi i čvorovi stanice; mjesta ukrcavanja perona, preko `parent_station_id`)
- `service_calendars` 1 → N `calendar_dates`
- `routes` 1 → N `trips`, `service_calendars` 1 → N `trips`, `shapes` 1 → N `trips`
- `trips` 1 → N `stop_times`, `stops` 1 → N `stop_times`
- `shapes` 1 → N `shape_points`
- `fares` 1 → N `fare_rules`, `routes` 1 → N `fare_rules`; zone u pravilima su vrijednosti `stops.zone_id`

## Podržano iz GTFS-a

| Datoteka | Polja |
|---|---|
| `agency.txt` | sva |
| `stops.txt` | sva osim `level_id` |
| `routes.txt` | sva |
| `trips.txt` | sva |
| `stop_times.txt` | sva osim `start/end_pickup_drop_off_window`, `pickup/drop_off_booking_rule_id` i `location_group_id`/`location_id` (GTFS-Flex) |
| `calendar.txt`, `calendar_dates.txt` | sva |
| `shapes.txt` | sva |
| `fare_attributes.txt`, `fare_rules.txt` | sva |
| `feed_info.txt` | sva; `feed_start_date`, `feed_end_date` i `feed_version` izračunavaju se pri izvozu |

Nisu podržani: `frequencies.txt` (uvoz takve vožnje pretvara u pojedinačne),
`transfers.txt`, `pathways.txt`, `levels.txt`, `translations.txt`,
`attributions.txt`, Fares v2 i GTFS-Flex.

## Razlike u odnosu na GTFS

**Surogat ključevi.** Svaka tablica ima `uuid` ključ, a GTFS identifikator
čuva se u stupcu `code`. GTFS ID se tako može mijenjati bez diranja veza, a
jedinstven je unutar tablice (`uq_<tablica>_code`).

**Javna oznaka stanice.** `stops.code` je GTFS `stop_id` (tehnički ključ, u
uvezenim feedovima često nečitljiv), a `stops.public_code` je GTFS `stop_code`,
oznaka koju putnik vidi na stupu.

**Jedna tvrtka po bazi.** Terminus je single-tenant, pa tablice nemaju tenant
ključ. `routes.agency_id` nije tenant nego GTFS operater linije.

**Vremena u sekundama.** `arrival_time` i `departure_time` spremaju se kao broj
sekundi od ponoći dana vožnje, umjesto teksta `HH:MM:SS`. Vožnje koje prelaze
ponoć imaju vrijednosti iznad 24 h, kao u GTFS-u (`25:30:00` = `91800`).
Pretvorba u `HH:MM:SS` radi se na rubovima sustava (API, izvoz).

**Vremena su obavezna na svakoj stanici.** GTFS dopušta prazna vremena na
stanicama koje nisu timepoint; Terminus ih uvijek ima (uvoz ih interpolira), a
okvirna vremena označava `timepoint = false`. Tako polasci, zasloni i PDF ne
moraju računati vremena.

**Datumi.** U bazi su tipa `date`; u GTFS izvozu se zapisuju kao `YYYYMMDD`.

**Boje.** `routes.color` i `routes.text_color` spremaju se kao hex bez znaka
`#`, velikim slovima (`1565C0`), kako ih GTFS i očekuje.

**Vrsta prijevoza.** `routes.route_type` prima osnovne GTFS vrijednosti (0–7,
11, 12) i proširene (100–1799, npr. 700 = autobus).

**Neobavezne vrijednosti.** `continuous_pickup` i `continuous_drop_off` su
`NULL` kad nisu zadane (u GTFS-u prazno polje), pa ponovni izvoz piše isto što je
uvezeno.

**Naziv kalendara.** `service_calendars.name` nije dio GTFS-a; to je naziv za
prikaz ("Radni dan", "Subota") i naslov stupca u PDF voznom redu.

**Aktivnost.** `stops.is_active` i `routes.is_active` nisu dio GTFS-a; služe za
privremeno isključivanje zapisa bez brisanja.

## Pravila koja provodi aplikacija

Ograničenja koja se mogu izraziti u bazi (raspon koordinata, dopuštene
vrijednosti enuma, redoslijed vremena …) postoje kao CHECK ograničenja.
Pravila koja zahtijevaju upit nad drugim recima provodi servisni sloj:

- nadređena stanica (`parent_station_id`) mora postojati i biti odgovarajuće vrste:

  | `location_type` | Nadređena |
  |---|---|
  | 0 stajalište/peron | neobavezno, stanica (1) |
  | 1 stanica | ne smije je imati |
  | 2 ulaz, 3 čvor | obavezno, stanica (1) |
  | 4 mjesto ukrcavanja | obavezno, peron (0) |

- koordinate su obavezne osim za čvorove (3) i mjesta ukrcavanja (4);
- stanici s podređenim zapisima ne može se promijeniti vrsta, a stajalište na
  kojem staju vožnje mora ostati stajalište;
- linija mora imati postojećeg prijevoznika (`agency_id`);
- linija, kalendar i trasa vožnje moraju postojati;
- vožnja ima barem dvije stanice; sve postoje i sve su stajališta ili peroni
  (`location_type = 0`);
- vremena ne idu unatrag: dolazak na stanicu nije prije polaska s prethodne;
  ni `shape_dist_traveled` se ne smanjuje;
- redne brojeve (`stop_sequence`, `shape_points.sequence`) dodjeljuje aplikacija
  prema redoslijedu (1, 2, 3 …); vremena vožnje i točke trase spremaju se uvijek
  kao cjelina;
- trasa ima barem dvije točke;
- `agencies.url` i `agencies.timezone` (IANA naziv) obavezni su jer ih GTFS traži;
- svi prijevoznici imaju istu vremensku zonu (GTFS to traži za feed, a polasci
  se računaju u toj zoni);
- cijena: valuta ISO 4217, cijena nije negativna, prijevoznik i linije u
  pravilima postoje, pravilo ima liniju ili barem jednu zonu.

## Brisanje

| Brisanje | Posljedica |
|---|---|
| prijevoznika | Odbija se (409) ako prijevoznik ima linije ili cijene (`RESTRICT`). |
| stanice | Podređeni zapisi ostaju bez nadređene stanice (`SET NULL`); odbija se (409) ako se stanica koristi u `stop_times`. |
| linije | Kaskadno briše njezine vožnje s vremenima i pravila cijena za tu liniju. |
| kalendara | Kaskadno briše iznimke; odbija se (409) ako ga koriste vožnje. |
| vožnje | Kaskadno briše njezina vremena. |
| trase | Kaskadno briše točke; vožnje ostaju bez trase (`SET NULL`). |
| cijene | Kaskadno briše njezina pravila. |
