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
| `settings` | Postavke instance (naziv, boje, logo, javni URL, objava GTFS-a) i vrijeme završetka čarobnjaka prvog pokretanja; najviše jedan redak. |
| `admin_users`, `admin_user_claims`, `admin_user_logins`, `admin_user_tokens` | Administratori (ASP.NET Core Identity, bez uloga). Korisničko ime je e-mail. |
| `data_protection_keys` | Ključevi za šifriranje kolačića prijave (ASP.NET Core Data Protection). |
| `timetable_seasons` | Sezone voznog reda: razdoblje, država praznika i pravilo za praznike. |
| `season_days` | Dani sezone koji odstupaju od dana u tjednu (praznici, ručne iznimke i dani razdoblja). |
| `season_periods` | Razdoblja sezone (npr. školski raspust) u kojima tip dana vozi kao drugi ili ne vozi. |
| `route_variants`, `route_variant_stops` | Varijante linije: redoslijed stanica smjera s vremenima vožnje i stajanja. |

Veze:

- `agencies` 1 → N `routes` (prijevoznik koji vozi liniju), `agencies` 1 → N `fares`
- `stops` 1 → N `stops` (peroni, ulazi i čvorovi stanice; mjesta ukrcavanja perona, preko `parent_station_id`)
- `service_calendars` 1 → N `calendar_dates`
- `timetable_seasons` 1 → N `service_calendars` (tipovi dana sezone), `timetable_seasons` 1 → N `season_days`
- `routes` 1 → N `route_variants` 1 → N `route_variant_stops`; `route_variants` 1 → N `trips` (`trips.variant_id`)
- vožnje istog polaska u različitim tipovima dana dijele `trips.departure_id` (bez posebne tablice)
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

Nisu podržani (vidi [GTFS uvoz](gtfs-import.md)): `frequencies.txt` (uvoz takve vožnje pretvara u pojedinačne),
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
prikaz ("Radni dan", "Subota").

**Sezone voznog reda.** Sezona (`timetable_seasons`) grupira kalendare kao
tipove dana: svaki tip dana je jedan kalendar s danima u tjednu i razdobljem
sezone (zadano radni dan pon–pet, subota, nedjelja). Dani koji odstupaju
(`season_days`) su državni praznici s [OpenHolidays API-ja](https://openholidaysapi.org)
(primjenjuje se pravilo sezone: vozi kao tip dana, ne vozi ili normalno) i ručne
iznimke s napomenom; ručna iznimka ima prednost pred praznikom. Iznimke tih
kalendara (`calendar_dates`) **izračunavaju se** iz dana sezone pri svakoj
promjeni (npr. "25. 12. kao nedjelja" je "ne vozi" za radni dan i "vozi" za
nedjelju), pa se tipovi dana sezone ne uređuju preko `/api/calendars` (409).
Svaki dan u tjednu pripada najviše jednom tipu dana, a sezone se ne preklapaju.
Kopija sezone (npr. isti vozni red sljedeće godine) dobiva nove kalendare s
istim tipovima dana i pravilom za praznike, a po želji i kopije svih vožnji
(nove vožnje s istim vremenima, varijantom i trasom), u jednoj transakciji;
ručne iznimke se ne kopiraju jer su vezane uz datume, a razdoblja se pomiču
koliko i sezona.

**Razdoblja.** Razdoblje (`season_periods`, npr. „Zimski raspust”, 23. 12. –
15. 1.) kaže da u njemu jedan tip dana (npr. „Radni dan – nastava”) vozi kao
drugi („Radni dan – raspust”) ili ne vozi; ostali tipovi dana voze normalno.
Tip dana za raspust obično nema nijedan dan u tjednu, pa vozi samo kroz
razdoblja. Iz razdoblja nastaju dani sezone s izvorom „razdoblje” (svaki dan
razdoblja u kojem zamijenjeni tip dana inače vozi), a iz njih iznimke kao i za
praznike. Ručna iznimka i praznik imaju prednost, pa Božić u raspustu vozi po
pravilu za praznike. Razdoblja istog tipa dana se ne preklapaju.
Kalendari uvezeni iz GTFS-a nisu u sezoni i ostaju kakvi jesu. GTFS uvoz briše
i sezone.

**Varijante linije.** Varijanta (`route_variants`) je redoslijed stanica
jednog smjera linije (npr. glavna Črnomerec → Sopot, skraćena, iz garaže), s
vremenom vožnje od prethodne stanice i stajanjem za svaku stanicu
(`route_variant_stops`), natpisom smjera i trasom. Nije dio GTFS-a: služi kao
predložak za nove vožnje, a vožnja (`trips.variant_id`) i dalje ima svoja
vremena, koja idu u izvoz. Prva varijanta smjera (`sort_order`) je glavna.
Varijante se mogu prepoznati iz postojećih vožnji: isti smjer i redoslijed
stanica čine varijantu, a vremena vožnje su medijan njezinih vožnji. Stanice
varijante koju koriste vožnje ne mogu se promijeniti, a takva varijanta ne
može se obrisati (409).

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
| prijevoznika | Odbija se (409) ako prijevoznik ima linije ili cijene (`RESTRICT`), osim uz `moveRoutesTo`: tada ih u istoj transakciji preuzima drugi prijevoznik. |
| stanice | Podređeni zapisi ostaju bez nadređene stanice (`SET NULL`); odbija se (409) ako se stanica koristi u `stop_times`. |
| linije | Kaskadno briše njezine vožnje s vremenima i pravila cijena za tu liniju. |
| kalendara | Kaskadno briše iznimke; odbija se (409) ako ga koriste vožnje. |
| vožnje | Kaskadno briše njezina vremena. |
| trase | Kaskadno briše točke; vožnje ostaju bez trase (`SET NULL`). Kad varijanta dobije novu trasu, dobivaju je i njezine vožnje, a stara se briše ako je više ništa ne koristi. |
| cijene | Kaskadno briše njezina pravila. |
