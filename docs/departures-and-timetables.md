# Polasci, zasloni na stanicama i PDF vozni redovi

> [!IMPORTANT]
> Ovo je plan: opisuje kako funkcije trebaju raditi i izgledati te redoslijed
> kojim ih treba izraditi. Kad se pojedini dio implementira, dokument se
> ažurira. Stanje po koracima je u [poglavlju 5](#5-redoslijed-implementacije).

## Pregled

Planirane su dvije funkcije za putnike:

1. **Zaslon na stanici.** Svaka stanica ima javni URL. Otvoren na ekranu na stanici
   (ili na mobitelu putnika), prikazuje trenutno vrijeme i nadolazeće polaske.
2. **PDF vozni red.** Administrator generira vozni red za jednu stanicu ili
   za sve stanice odjednom. PDF se ispisuje i postavlja na stanicu.

Obje funkcije počivaju na istom temelju, izračunu polazaka, pa se on radi prvi i
mora biti dobro pokriven testovima:

```
                    ┌──────────────────────────────┐
                    │      Izračun polazaka        │
                    │  (aktivni kalendari + vožnje │
                    │   kroz stanicu u rasponu)    │
                    └──────────────┬───────────────┘
                   ┌───────────────┴───────────────┐
                   ▼                               ▼
         Zaslon na stanici                 PDF vozni red
   (sljedećih N polazaka od "sada")   (svi polasci po danima u tjednu)
```

---

## 1. Temelj: izračun polazaka

### 1.1 Koji kalendari voze na zadani datum

Kalendar (`service_calendars`) vozi na datum `D` ako vrijedi **jedno** od sljedećeg:

- za `D` postoji iznimka `exception_type = 1` (dodatno vozi), **ili**
- `D` je unutar razdoblja `start_date`–`end_date`, dan u tjednu od `D` je označen
  i za `D` **ne** postoji iznimka `exception_type = 2` (ne vozi).

Primjer: kalendar "Radni dan", pon–pet, 1.1.–31.12.2026., s iznimkom
"ne vozi 25.12.":

| Datum | Dan | Vozi? | Zašto |
|---|---|---|---|
| 23.12.2026. | srijeda | da | radni dan u razdoblju |
| 25.12.2026. | petak | **ne** | iznimka "ne vozi" |
| 26.12.2026. | subota | ne | subota nije označena |
| 2.1.2027. | subota | ovisi | samo ako postoji iznimka "vozi" za 2.1. |

### 1.2 Vožnje preko ponoći

GTFS vremena odnose se na **dan voznog reda**, ne na kalendarski dan, i mogu biti
veća od 24 h. Vožnja radnog dana u srijedu s polaskom `24:20:00` stvarno kreće u
**četvrtak u 00:20**.

Posljedica: kad zaslon u četvrtak u 00:10 traži sljedeće polaske, mora gledati
dva dana voznog reda:

- **četvrtak**: vožnje s vremenima od 00:10 nadalje;
- **srijedu**: vožnje s vremenima od 24:10 nadalje (to su četvrtak 00:10+).

### 1.3 Vremenska zona

"Sada" je uvijek trenutno vrijeme u zoni prijevoznika (`agencies.timezone`, npr.
`Europe/Zagreb`; GTFS traži da svi prijevoznici u feedu imaju istu zonu), a ne vrijeme poslužitelja (koji je često u UTC-u). Pretvorba
"dan voznog reda + sekunde od ponoći" u stvarni trenutak mora ispravno obraditi
prijelaze na ljetno i zimsko računanje vremena (GTFS vremena računaju se od
"podneva minus 12 h", što je bitno upravo za te dane).

### 1.4 Stanice s peronima

Ako je stanica kolodvor (`location_type = 1`), prikazuju se polasci sa **svih
njezinih perona**, uz oznaku perona. Ako je stanica peron ili obično stajalište,
prikazuju se samo njezini polasci.

### 1.5 Što se ne prikazuje kao polazak

- **Zadnja stanica vožnje.** Tamo autobus samo stiže i ne polazi dalje.
- **Stanice bez ukrcavanja** (`pickup_type = 1`), npr. stanica gdje putnici
  smiju samo izaći.
- **Neaktivne linije** (`routes.is_active = false`), kao i kod GTFS izvoza.

### 1.6 Podaci o jednom polasku

| Polje | Izvor |
|---|---|
| Linija | `routes.short_name` (ako ga nema, `long_name`) |
| Boja linije | `routes.color` i `routes.text_color` |
| Smjer | `stop_times.stop_headsign`; ako ga nema, `trips.trip_headsign`; ako ni toga nema, naziv zadnje stanice vožnje |
| Vrijeme polaska | stvarni datum i vrijeme u zoni prijevoznika |
| Peron | `stops.platform_code` (kod kolodvora) |

Upit koristi postojeći indeks `ix_stop_times_stop_departure (stop_id, departure_time)`,
koji je za ovu namjenu i napravljen.

### 1.7 Implementacija

| Dio | Gdje |
|---|---|
| Vozi li kalendar na datum (1.1) | `ServiceCalendarRules` (Terminus.Service) |
| Dan voznog reda → stvarno vrijeme, "podne minus 12 h" (1.2, 1.3) | `ServiceDay` (Terminus.Common) |
| Upit polazaka, filteri iz 1.5, smjer iz 1.6 | `DepartureRepository.ScheduledQuery` |
| Sljedeći polasci sa stanice ili kolodvora (1.4) | `DepartureService.GetNextAsync` |

`DepartureService.GetNextAsync(stopId, now, limit)` traži polaske u idućih 24 h
(najviše 30). Gleda dane voznog reda od prekjučer (vremena do 48 h) do dana na
koji pada kraj tog razdoblja. Za svaki dan određuje aktivne kalendare i raspon
GTFS vremena koji pada u traženi period, a zatim spaja i sortira rezultate.
Vremenska zona je zona prvog unesenog prijevoznika; servis za prijevoznike ne
dopušta različite zone.

---

## 2. Zaslon na stanici

### 2.1 URL

Svaka stanica dobiva stalnu javnu adresu, primjerice:

```
https://<domena>/board/OS-AK
                       └─┬─┘
                      stanica
```

Svaka tvrtka ima vlastitu instancu (i domenu), a `stops.code` je jedinstven u
bazi, pa kod stanice sam određuje adresu. Nova migracija nije potrebna.

### 2.2 API

```
GET /api/public/departures/{stopCode}?limit=10
```

- Javni endpoint, **bez prijave**.
- `limit`: koliko polazaka vratiti (zadano 10, najviše 30; inače 400).
- Nepoznat kod stanice vraća 404, a instanca bez prijevoznika 422.
- `routeName` je kratka oznaka linije, a ako je nema, puni naziv.
- `minutesUntil` su cijele minute do polaska, zaokružene nadolje.
- Implementacija: `PublicDeparturesController` (trenutno vrijeme daje
  `TimeProvider`) poziva `DepartureService.GetNextAsync(stopCode, …)`.

Primjer odgovora:

```json
{
  "stop": {
    "code": "OS-AK",
    "name": "Osijek, Autobusni kolodvor"
  },
  "agencyName": "Test prijevoznik d.o.o.",
  "now": "2026-12-23T06:58:12+01:00",
  "departures": [
    {
      "routeName": "12",
      "routeColor": "1565C0",
      "routeTextColor": "FFFFFF",
      "headsign": "Đakovo",
      "platform": "3",
      "departureTime": "2026-12-23T07:05:00+01:00",
      "minutesUntil": 6
    },
    {
      "routeName": "7",
      "routeColor": "2E7D32",
      "routeTextColor": "FFFFFF",
      "headsign": "Vukovar",
      "platform": "5",
      "departureTime": "2026-12-23T07:15:00+01:00",
      "minutesUntil": 16
    }
  ]
}
```

### 2.3 Izgled zaslona

Zaslon je javna ruta `/board/{stopCode}` u frontend aplikaciji (React, TypeScript,
Vite; ista aplikacija kao dashboard). Ruta se učitava zasebno (lazy loading), bez
koda dashboarda i bez prijave. Radi na velikim ekranima u vodoravnom i okomitom
položaju (monitori na ugibalištima) i čitljiva je na mobitelu. Logo i boje
dolaze iz postavki instance (`GET /api/public/settings`).

```
┌──────────────────────────────────────────────────────────────┐
│  Osijek, Autobusni kolodvor                          06:58   │
├──────────────────────────────────────────────────────────────┤
│  [12]  Đakovo                     peron 3     07:05   6 min  │
│  [ 7]  Vukovar                    peron 5     07:15  16 min  │
│  [12]  Đakovo                     peron 3     07:35  36 min  │
│  [21]  Našice                     peron 1     07:40  41 min  │
│  [ 7]  Vukovar                    peron 5     08:15   1 h    │
├──────────────────────────────────────────────────────────────┤
│  Vremena su prema voznom redu.                    Terminus   │
└──────────────────────────────────────────────────────────────┘
```

- Oznaka linije u boji linije (`routeColor` / `routeTextColor`).
- Sat se osvježava svake sekunde na klijentu; polasci se dohvaćaju svakih ~30 s.
- Polasci unutar ~1 minute se ističu (npr. "polazi"), a prošli nestaju.
- Kad nema polazaka do kraja dana: poruka "Nema više polazaka danas" i prvi
  sljedeći polazak.
- Ako dohvat ne uspije (nema mreže), prikazuju se zadnji poznati podaci uz
  diskretnu oznaku da nisu osvježeni.
- Sat i "za N min" računaju se prema `now` iz odgovora API-ja, a ne prema satu
  uređaja (ekrani na stanicama često imaju krivo podešen sat).
- Monitor radi danima bez restarta: stranica se povremeno (npr. jednom dnevno)
  sama potpuno osvježi, a dohvat ne smije gomilati memoriju.
- Opcije u URL-u, npr. `/board/OS-AK?limit=8`.

### 2.4 Opterećenje i zaštita

Zasloni na svim stanicama neprestano dohvaćaju iste podatke. Zato:

- **Output caching**: odgovor za istu stanicu sprema se ~30 s, pa većina zahtjeva
  ne ide u bazu.
- **Rate limiting** na `/api/public/*`: ograničenje broja zahtjeva po IP adresi.

### 2.5 Ograničenje

Zaslon prikazuje **vremena iz voznog reda**, a ne stvarni položaj vozila. Ako
autobus kasni, zaslon to ne zna. Prikaz stvarnih dolazaka zahtijevao bi praćenje
vozila (GPS) i GTFS-Realtime, što nije dio ovog plana.

---

## 3. PDF vozni red za stanicu

### 3.1 Izgled

Jedna stranica po stanici (A4, uspravno), klasičan oblik voznog reda na stanicama:

```
┌──────────────────────────────────────────────────────────┐
│  OSIJEK, AUTOBUSNI KOLODVOR                    [QR kod]  │
│  Vozni red vrijedi od 1.1.2026. do 31.12.2026.           │
├──────────────────────────────────────────────────────────┤
│  [12]  smjer Đakovo                                      │
│                                                          │
│        Radni dan          Subota         Nedjelja i      │
│                                          praznici        │
│     5 | 45             6 | 30          7 | 30            │
│     6 | 15  45         7 | 30         10 | 30            │
│     7 | 05  35  55a    9 | 30         14 | 30            │
│     8 | 35            12 | 30         18 | 30            │
│    ...                ...                                │
├──────────────────────────────────────────────────────────┤
│  [ 7]  smjer Vukovar                                     │
│     ...                                                  │
├──────────────────────────────────────────────────────────┤
│  a – ne vozi 24.12. i 31.12.                             │
│  Ne vozi 25.12. i 1.1.                                   │
│  Skenirajte QR kod za polaske uživo.   Test prijevoznik  │
└──────────────────────────────────────────────────────────┘
```

### 3.2 Pravila sadržaja

- **Zaglavlje**: naziv stanice, razdoblje važenja (od najranijeg do najkasnijeg
  datuma kalendara koji se prikazuju) i QR kod.
- **Grupiranje**: po liniji i smjeru, a unutar toga po kalendaru. Stupci su nazivi
  kalendara (`service_calendars.name`), npr. "Radni dan", "Subota",
  "Nedjelja i praznici", pa ih prijevoznik imenuje kako želi da piše na voznom redu.
- **Rešetka sat | minute**: svaki redak je jedan sat, a u njemu minute polazaka.
- **Napomene**:
  - iznimke koje vrijede za cijeli kalendar ispisuju se ispod tablice
    (npr. "Ne vozi 25.12. i 1.1.");
  - polasci koji se razlikuju od ostalih označavaju se slovom uz minutu i
    objašnjavaju u napomeni (npr. `55a` – "ne vozi 24.12. i 31.12.").
- **Kolodvori**: za stanicu s peronima uz smjer se navodi i peron.
- **Podnožje**: naziv prijevoznika i uputa za QR kod.
- **QR kod** vodi na zaslon te stanice (poglavlje 2), pa putnik na mobitelu vidi
  sljedeće polaske.

### 3.3 API (samo administrator)

```
GET /api/stops/{stopId}/timetable.pdf
GET /api/timetables.pdf
```

- Prvi vraća PDF za jednu stanicu.
- Drugi vraća **jedan PDF sa svim aktivnim stanicama**, po jednu stranicu za
  stanicu, spreman za ispis odjednom. Može se proširiti filtrom (npr. samo
  stanice jedne linije).

PDF se generira tijekom zahtjeva. Za nekoliko stotina stanica to su sekunde; ako
trajanje postane problem, generiranje se premješta u pozadinski posao s
preuzimanjem kad je gotov.

### 3.4 Biblioteke

- [QuestPDF](https://www.questpdf.com/) za izradu PDF-a C# kodom. Community licenca
  je besplatna za organizacije s godišnjim prihodom ispod 1 milijun USD; uvjete
  treba provjeriti prije produkcije.
- QRCoder za QR kod.

Alternativa je HTML predložak pretvoren u PDF headless preglednikom (Playwright):
lakše za dizajn, ali zahtjevnije za postavljanje na poslužitelju.

---

## 4. Preduvjeti i arhitektura

- **Testovi.** Izračun aktivnih kalendara i polazaka pokriven je unit testovima
  (rubni slučajevi: ponoć, ljetno računanje vremena, iznimke, kolodvori).
- **Frontend aplikacija.** Prijevoznik upravlja svime kroz dashboard (stanice,
  linije, kalendari, vožnje, postavke, pokretanje izrade PDF-a). Ista aplikacija
  sadrži i javni zaslon stanice. Frontend je u istom repozitoriju kao backend
  (`frontend/` uz `backend/`) i koristi TypeScript klijent generiran iz
  OpenAPI opisa API-ja (`/openapi/v1.json`).
- **Autentikacija.** Sve osim `/api/public/*` traži prijavu. Predviđa se više
  administratora po instanci; uloge se mogu proširiti kasnije.
- **CORS / isti origin.** U razvoju frontend radi na vlastitoj adresi (Vite), pa
  API mora dopustiti tu adresu. U produkciji su frontend i API iza iste domene.
- **Postavke instance.** Logo, boje i podaci za `feed_info.txt` (izdavač, kontakt)
  čuvaju se u bazi (jedan redak, tablica `settings`) i uređuju u dashboardu.

## 5. Redoslijed implementacije

| # | Korak | Ovisi o |
|---|---|---|
| 1 | Testni projekt (xUnit) i CI ✅ | — |
| 2 | Izračun aktivnih kalendara i polazaka, s testovima ✅ | 1 |
| 3 | Javni endpoint polazaka ✅ | 2 |
| 4 | Autentikacija (više administratora) i CORS | — |
| 5 | Postavke instance (`settings`: logo, boje, `feed_info`) | 4 |
| 6 | Output caching i rate limiting za `/api/public/*` | 3 |
| 7 | Frontend: kostur aplikacije, prijava, generirani API klijent | 4 |
| 8 | Dashboard: stanice, linije, kalendari, vožnje | 7 |
| 9 | Zaslon na stanici `/board/{stopCode}` | 5, 6, 7 |
| 10 | PDF za jednu i za sve stanice, pokretanje iz dashboarda, QR kod na zaslon | 8, 9 |

## 6. Otvorena pitanja

- **Format PDF-a**: samo A4 ili i veći formati (A3) za kolodvore s mnogo linija?
- **Dolasci**: prikazuje li zaslon i dolaske na zadnju stanicu (npr. na kolodvoru),
  ili samo polaske?
- **Jezik**: treba li zaslon i PDF i na engleskom (turističke linije)?
