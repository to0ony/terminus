# Polasci, zaslon na stanici i PDF vozni red

Funkcije za putnike: javni **zaslon na stanici** sa sljedećim polascima i
**PDF vozni red** stanice za ispis. Obje počivaju na istom izračunu polazaka iz
voznog reda (kalendari, vožnje preko ponoći, vremenska zona, kolodvori):

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

Sadržaj:

1. [Izračun polazaka](#1-izračun-polazaka)
2. [Zaslon na stanici](#2-zaslon-na-stanici)
3. [PDF vozni red](#3-pdf-vozni-red)
4. [Karta uživo](#4-karta-uživo)
5. [Odluke](#5-odluke)

---

## 1. Izračun polazaka

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

### 2.1 Adresa

Svaka stanica ima stalnu javnu adresu po GTFS `stop_id` (`stops.code`):

```
https://<domena>/board/OS-AK?limit=8&past=2
                       └─┬─┘ └──┬──┘ └──┬─┘
                      stanica   │       otišli polasci (zadano 2, 0 ih skriva, najviše 5)
                                broj polazaka (zadano 12, najviše 30)
```

Svaka tvrtka ima vlastitu instancu (i domenu), a `stops.code` je jedinstven,
pa kod stanice sam određuje adresu. Na stranici stanice u dashboardu gumb
**Zaslon polazaka** otvara tu adresu.

### 2.2 API

```
GET /api/public/departures/{stopCode}?limit=10
```

- Javni endpoint, **bez prijave**.
- `limit`: koliko polazaka vratiti (zadano 10, najviše 30; inače 400).
- `past`: koliko upravo otišlih polazaka (zadnjih pola sata) vratiti u
  `recent`, najstariji prvi (zadano 0, najviše 5). Zaslon ih traži 2.
- `serviceDate` je dan voznog reda polaska: vožnja iza ponoći (GTFS 24:xx)
  pripada danu ranije. Po njemu zaslon označava kraj dana.
- Nepoznat kod stanice vraća 404, a instanca bez prijevoznika 422.
- `routeName` je kratka oznaka linije, a ako je nema, puni naziv.
- `minutesUntil` su cijele minute do polaska, zaokružene nadolje. Odgovor može
  biti iz cachea (do 20 s), pa zaslon minute računa sam iz `departureTime`.
- Implementacija: `PublicDeparturesController` (trenutno vrijeme daje
  `TimeProvider`) poziva `DepartureService.GetNextAsync(stopCode, …)`.

Primjer odgovora:

```json
{
  "stop": { "code": "OS-AK", "name": "Osijek, Autobusni kolodvor" },
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
      "minutesUntil": 6,
      "serviceDate": "2026-12-23"
    }
  ],
  "recent": []
}
```

### 2.3 Izgled i ponašanje

Zaslon je javna ruta `/board/:stopCode` u frontend aplikaciji
(`frontend/src/features/board`). Učitava se zasebno, bez koda dashboarda i bez
Mantine komponenti (vlastiti CSS), pa je brz i na slabijim uređajima.

```
┌──────────────────────────────────────────────────────────────┐
│ [logo] Zagrebački električni tramvaj                         │
│        Stanica Trg bana J. Jelačića                   20:56  │
├──────────────────────────────────────────────────────────────┤
│ ┆[ 6]  Črnomerec                          20:50 (otišao) ┆  │
│  [17]  Borongaj                                      stiže   │
│  [13]  Žitnjak                                    za 1 min   │
│  [ 6]  Črnomerec                                  za 3 min   │
│  [14]  Mihaljevac                                    21:14   │
│        Zadnji polazak                                        │
│ ━━ Kraj voznog dana ━━━━━━━━━━━━━ Sljedeći polasci: sutra ━━ │
│  [17]  Borongaj                                      04:30   │
└──────────────────────────────────────────────────────────────┘
```

- Zaglavlje: logo, naziv i boje iz postavki instance (`GET /api/public/settings`).
- Redak: oznaka linije u boji linije, odredište, peron (kod kolodvora) i vrijeme:
  "stiže" (manje od minute), "za X min" (do 10 minuta), inače sat polaska.
- Polasci se dohvaćaju svakih 30 s, a sat i minute osvježavaju se svake sekunde.
- Minute se računaju po satu **poslužitelja** (`now` i zaglavlje `Age` iz
  odgovora), jer zasloni na stanicama često imaju krivo namješten sat. Polasci
  koji su u međuvremenu otišli prelaze među otišle i prije sljedećeg dohvata.
- Na vrhu su do 2 upravo otišla polaska (zadnjih pola sata, `?past=`),
  zatamnjeni, crtkani i s precrtanim vremenom i „otišao”, da je jasno da više
  nisu bitni.
- **Kraj dana voznog reda** je crta „Kraj voznog dana · Sljedeći polasci:
  sutra” (ili „danas” iza ponoći, odnosno dan i datum) između dva dana; kad
  danas više nema polazaka, na vrhu piše „Danas više nema polazaka”. Vožnje
  iza ponoći (24:xx) ostaju u svom danu. Zadnji polazak linije u smjeru tog
  dana ima oznaku „Zadnji polazak”, ali samo kad popis sigurno pokriva ostatak
  dana (vidi se idući dan ili je popis kraći od traženog), da se ne pogađa.
- Ako dohvat ne uspije, ostaju zadnji poznati polasci, a nakon 1,5 minute
  ispod njih piše od kada nisu osvježeni.
- Veličina slova prati širinu i visinu ekrana: 12 polazaka stane na položeni
  zaslon bez pomicanja, a radi i na mobitelu. Tema prati postavku uređaja.
- Nepoznata stanica: "Stanica „X” ne postoji."; bez polazaka: "Nema polazaka u
  sljedeća 24 sata."

### 2.4 Opterećenje i zaštita

Zasloni na svim stanicama neprestano dohvaćaju iste podatke, pa su javni
endpointi zaštićeni (`Terminus.WebApi/Infrastructure/PublicApi.cs`):

| Endpoint | Cache na poslužitelju | Granica po IP adresi |
|---|---|---|
| `/api/public/departures/{stopCode}` | 20 s (po stanici, `limit` i `past`) | 120 zahtjeva u minuti |
| `/api/public/settings` | 5 min | dijeli granicu s polascima |
| `/api/public/settings/logo` | u pregledniku (verzija u adresi) | dijeli granicu s polascima |
| `/api/public/gtfs` | 10 min | 5 zahtjeva u minuti |

- Spremanje postavki ili loga briše cache postavki i feeda; GTFS uvoz briše
  cache polazaka, postavki i feeda.
- Iznad granice odgovor je `429` s `Retry-After` i ProblemDetails.
- Granice se mijenjaju konfiguracijom `PublicApi` (vidi
  [konfiguraciju](development.md#konfiguracija)).
- Iza reverse proxyja granica se računa po adresi proxyja dok se ne uključe
  forwarded headers (dio deploya, faza 6 u [roadmapu](roadmap.md)).

### 2.5 Ograničenje

Zaslon prikazuje **vremena iz voznog reda**, a ne stvarni položaj vozila. Ako
autobus kasni, zaslon to ne zna. Za stvarne dolaske trebali bi praćenje vozila
(GPS) i GTFS-Realtime.

---

## 3. PDF vozni red

### 3.1 Izgled

Klasičan vozni red na stanici, uspravno, **A4** (odjeljci u dva stupca) ili
**A3** (tri stupca, za kolodvore s mnogo linija), na **hrvatskom ili
engleskom**:

```
┌──────────────────────────────────────────────────────────┐
│  Gradski prijevoz Osijek                       [QR kod]  │
│  Osijek, Autobusni kolodvor                Polasci uživo │
│  Vrijedi od 1. 12. 2026. do 30. 6. 2027.                 │
├──────────────────────────────────────────────────────────┤
│  [12]  smjer Đakovo  ·  peron 3                          │
│  sat │ Radni dan        │ Subota      │ Nedjelja         │
│    5 │ 45               │             │                  │
│    6 │ 15  45           │ 30          │                  │
│    7 │ 05  35  55a      │ 30          │ 30               │
│  ...                                                     │
│  a – ne vozi 24. 12. i 31. 12.                           │
│  25. 12. i 1. 1. vozi kao „Nedjelja”.                    │
├──────────────────────────────────────────────────────────┤
│  Gradski prijevoz Osijek · Vremena su prema voznom redu. │
└──────────────────────────────────────────────────────────┘
```

### 3.2 Pravila sadržaja

- **Razdoblje** bira administrator. Zadano je od danas do zadnjeg dana s
  voznim redom; za vozni red koji tek počinje vrijediti bira se kasniji
  početak. Najviše 366 dana.
- **Zaglavlje**: logo instance (naziv samo kad nema loga), „Stanica” i naziv stanice
  podebljano, razdoblje i QR kod.
- **Odjeljci** po liniji i smjeru (natpis smjera vožnje), a kod kolodvora i po
  peronu. Poredani su po oznaci linije (2 prije 11).
- **Stupci** su dani u tjednu s istim voznim redom. Ne uzimaju se iz naziva
  kalendara, nego iz toga koji polasci stvarno voze na koji datum: za svaki dan
  u tjednu uzima se najčešći skup polazaka u razdoblju, a dani s istim skupom
  čine stupac ("Radni dan", "Subota", "Nedjelja", "Pon–Pet, Ned" itd.). Tako
  radi i s feedovima koji cijeli raspored drže u `calendar_dates.txt` (npr. ZET),
  a naslovi stupaca postoje na oba jezika.
- **Rešetka sat | minute**: svaki redak je jedan sat. Vožnje iza ponoći
  pripadaju danu na koji su krenule i dolaze iza 23 h.
- **Napomene** za datume koji odstupaju od svog dana u tjednu:
  - bez polazaka: "Ne vozi 18. 11. i 25. 12.";
  - s voznim redom drugog stupca: "25. 12. vozi kao „Nedjelja”.";
  - inače datum ide u stupac s kojim dijeli najviše polazaka, a polasci koji ne
    voze na sve njegove datume dobivaju slovo uz minutu (`55a` – "ne vozi
    24. 12. i 31. 12." ili "vozi samo 26. 12.").
  - Datumi napomene spajaju se u **raspone** preko dana koji napomeni ne
    proturječe: u napomeni stupca „Radni dan” vikend ne prekida raspon, pa je
    ljetni raspust „15. 6. – 4. 9.”, a ne 12 tjednih komadića. Raspon prekida
    samo dan na koji polazak (ili linija, kod „ne vozi”) vozi.
  - Kad su u napomeni **svi dani nekog naziva** iz sezone (razdoblje, praznik
    ili ručna iznimka s napomenom), piše se naziv s rasponom: „a – ne vozi:
    Ljetni raspust (15. 6. – 4. 9.) i Zimski raspust (21. 12. – 31. 12.)”,
    „Ne vozi: Dan grada (9. 12.)”. Ostali datumi ostaju datumi, pa uvezeni
    GTFS bez sezona dobiva samo spojene raspone.
- **QR kod** vodi na zaslon stanice: javna adresa iz postavki +
  `/board/{stopCode}`. Bez javne adrese PDF nema QR kod (dijalog na to upozorava).
- **Podnožje**: naziv instance, "Vremena su prema voznom redu." i broj stranice.

### 3.3 Preuzimanje

Na stranici stanice u dashboardu gumb **PDF vozni red** otvara dijalog s
razdobljem, formatom i jezikom. API (samo administrator):

```
GET /api/stops/{stopId}/timetable.pdf?from=2026-12-01&to=2027-06-30&paper=A3&language=en
```

- Svi parametri su neobavezni (zadano: danas do kraja voznog reda, A4, `hr`).
- 400 za neispravno razdoblje, 422 ako u razdoblju sa stanice nema polazaka.
- PDF se izrađuje tijekom zahtjeva; za veliku stanicu (ZET, Trg bana J.
  Jelačića, 1.374 polaska dnevno) to traje manje od sekunde.

U planu je i jedan PDF za sve stanice odjednom (vidi [roadmap](roadmap.md)).

### 3.4 Implementacija

| Dio | Gdje |
|---|---|
| Razdoblje, polasci stanice ili kolodvora, provjere | `StopTimetableService` (Terminus.Service) |
| Odjeljci, stupci i napomene (3.2) | `StopTimetableBuilder`, testovi u `StopTimetableBuilderTests` |
| Izgled PDF-a | `Pdf/StopTimetableDocument` ([QuestPDF](https://www.questpdf.com/); font Lato dolazi s paketom) |
| Tekstovi na hrvatskom i engleskom | `Pdf/TimetableTexts` |
| QR kod (SVG) | [QRCoder](https://github.com/codebude/QRCoder) |

### 3.5 Shema linije (PDF)

PDF s redoslijedom stanica linije, za ispis u vozilu ili na stanici. Na
stranici linije u dashboardu: izbornik **⋯ → PDF shema linije** (A4/A3,
hrvatski/engleski, minute po izboru).

```
GET /api/routes/{routeId}/diagram.pdf?paper=A4&language=hr&showMinutes=false
```

- Crta se iz **glavnih varijanti** (prva po redoslijedu) smjera 0 i smjera 1.
  Smjer 1 se obrne i spoji sa smjerom 0 (najdulji zajednički podniz, kao
  redci grafikona). Zatim se redom umeću **stanice ostalih varijanti** (onih
  smjera 1 obrnuto), između najbližih zajedničkih stanica; tako je na shemi
  svaka stanica koju koristi barem jedna varijanta. Stanica izvan trase (npr.
  garaža) završi uz stanicu s kojom se varijanta spaja.
- Stanice se spajaju **po nazivu** (bez razlike u velikim slovima i
  razmacima), jer su stanice suprotnih smjerova obično zasebni zapisi s dvije
  strane ulice.
- Stranica je **položena**: stanice su točke na vodoravnoj crti u boji
  linije, nazivi koso iznad nje. Naslov je naziv linije (`route_long_name`);
  ispod su krajevi glavne trase i broj stanica.
- **Podebljane** su početne i završne stanice glavnih varijanti (veća točka)
  i stanice gdje ostale varijante počinju ili završavaju.
- Stanica gdje staje samo jedan smjer ima strelicu smjera u točki. Smjer
  staje na stanici ako je ima **bilo koja varijanta** tog smjera (po nazivu),
  ne samo glavna; minute su ipak samo za glavnu varijantu.
- Duga linija se **lomi u više crta** koje se nastavljaju kao zmija, sa
  zaobljenim zaokretom: druga crta ide zdesna nalijevo, s nazivima ispod.
  Shema je uvijek na jednoj stranici; ako ni tako ne stane, smanji se.
- `showMinutes=true`: uz svaku stanicu minute od polazne stanice za oba
  smjera (smjer 1 sivo); kod linija s okvirnim vremenima međustanice imaju
  `~`. Bez minuta nema ni napomene o vremenima.
- 422 ako linija nema varijantu sa stanicama.

**Sheme svih linija sa stanice.** Na stranici stanice izbornik **PDF → Sheme
linija sa stanice** izradi jedan PDF sa shemom svake linije koja staje na
stanici (za kolodvor na bilo kojem peronu), svaka na svojoj stranici. Stanica
je na svakoj shemi istaknuta (puna tamna točka, naziv na obojenoj podlozi);
kod kolodvora i peroni, po nazivu.

```
GET /api/stops/{stopId}/routes.pdf?paper=A4&language=hr&showMinutes=true&minutesFromStop=true
```

Uz `minutesFromStop=true` minute se računaju **od te stanice**: u svakom
smjeru do stanica iza nje, a stanice prije nje (u tom smjeru) su bez minuta.
Računa se razlika zaokruženih minuta od polazne stanice, pa može odstupati za
minutu. Smjer koji na stanici ne staje nema minuta. Kod sheme jedne linije
opcija se zanemaruje.

| Dio | Gdje |
|---|---|
| Spajanje smjerova i minute | `RouteDiagramBuilder`, testovi u `RouteDiagramBuilderTests` |
| Izgled (crta kao SVG, nazivi preko nje) | `Pdf/RouteDiagramDocument`, tekstovi u `Pdf/RouteDiagramTexts` |

### 3.6 Licenca za QuestPDF

QuestPDF Community licenca (verzija 3.0, srpanj 2026.) besplatna je među
ostalim za open-source projekte s OSI licencom, organizacije s godišnjim
prihodom ispod 1 milijun USD i korisnike čiji kod ne poziva QuestPDF izravno.
**Javni sektor i državne agencije nisu obuhvaćeni bez obzira na prihod**, a
gradski prijevoznici često su u javnom vlasništvu. Prije produkcije treba
provjeriti koja licenca vrijedi. Sav kod koji poziva QuestPDF je u
`StopTimetableDocument` i `RouteDiagramDocument`, pa se biblioteka može zamijeniti (npr. PDFsharp i
MigraDoc, MIT licenca).

---

## 4. Karta uživo

Na **Pregledu** gumb **Karta uživo** (`/live`) otvara kartu u stilu Google
karata s plavim stanicama. Klik na stanicu prikazuje **polaske do kraja
dana** redom (linija, smjer, vrijeme, „za X min”). Klik na polazak prikazuje
**trasu tog smjera** (vožnje linije s istim smjerom i natpisom) u boji linije,
okrugla **vozila koja upravo voze** i gore **ostala vremena** tog smjera sa
stanice. Vozila su **prema voznom redu** (simulacija), ne prema GPS-u.

- Podloga je zadano **detaljna (Liberty)**; može se promijeniti, a izbor se
  pamti samo za ovu kartu.
- **Uživo**: današnji dan i trenutno vrijeme u zoni prijevoznika.
- **Simulacija**: odabrani dan, vrijeme (klizač) i brzina od 1× do 600×; služi
  za provjeru voznog reda.
- Ispred svake linije je ikona vrste prijevoza (autobus, vlak, brod…).
- Klik na vozilo (na karti ili u popisu): karta mu se približi i **prati
  ga**; na mobitelu se ploča spusti. Povlačenje karte prekida praćenje, a gumb
  **Prati vozilo** ga vraća.
- **Sva vozila** (prekidač uz sat): na karti su vozila svih linija, svako u
  boji svoje linije; klik na vozilo prikazuje njegovu vožnju (linija, smjer,
  sljedeće stanice). Podaci: `GET /api/routes/live?date=`.
- **Javna karta** `/karta`, bez prijave: ista karta, samo uživo (bez
  simulacije), s nazivom i logom iz postavki. Uključuje se u **Postavkama →
  Objavi kartu uživo** (`settings.public_map_enabled`); dok je isključena,
  `/api/public/map/*` vraća 404. Javni API (`/api/public/map/stops/area`,
  `stops/extent`, `stops/{id}/departures`, `stops/{id}/routes`,
  `routes/{id}/live`, `live`) daje samo aktivne stanice, s ograničenjem
  zahtjeva i cacheom od 5 minuta.
  Javna karta ima i gumb **moja lokacija** (ispod zuma): plava točka
  korisnika, uz dopuštenje preglednika; radi samo preko HTTPS-a (ili na
  localhostu).
- **Otvori u novoj kartici** otvara `/live/full`: ista karta preko cijelog
  prozora, bez izbornika i zaglavlja dashboarda (traži prijavu kao i
  dashboard).
- Na mobitelu je panel **donja ploča** koja se skuplja i širi (ručka na
  vrhu), sat i simulacija su gore, a karta se trasi prilagođava oko njih.
- Kod odabranog polaska vremena smjera su u jednom redu (ranija blago siva,
  bira se klikom ili strelicama), a ispod su **stanice tog polaska**: ova
  stanica, tri sljedeće i zadnja, s vremenom dolaska i koliko se vozi od ove
  stanice; ostale i ranije stanice otvaraju se na klik. Promjena vremena gore mijenja vremena stanica, a
  vozilo tog polaska vidi se na crti stanica.
- Polasci dolaze iz `GET /api/stops/{id}/departures?date=` (kao zaslon
  stanice); noćne vožnje prethodnog dana nisu na popisu polazaka, ali se na
  trasi vide kao vozila.

```
GET /api/routes/{routeId}/live?date=2026-10-26
```

Vraća vožnje linije koje voze taj dan (i vožnje prethodnog dana koje traju
iza ponoći, s vremenima pomaknutim za dan), s vremenima u sekundama od
ponoći, trasama i stanicama s koordinatama. Vožnja bez trase uzima trasu
svoje varijante; bez obje vozilo ide ravno od stanice do stanice.

Položaj računa preglednik: na stanici između dolaska i polaska, a između
stanica jednoliko duž trase od polaska do dolaska. Stanice se postavljaju na
trasu redom, pa i odvojak tamo i natrag ispravno ide.

| Dio | Gdje |
|---|---|
| Vožnje dana | `LiveMapService`, testovi u `LiveMapServiceTests` |
| Simulacija i sat | `features/live/simulation.ts`, `clock.ts`, `useSimulationClock.ts` |
| Karta i paneli | `features/live/LiveMap.tsx`, `LiveMapPage.tsx` |

## 5. Odluke

| Tema | Odluka |
|---|---|
| Adresa zaslona | Po GTFS `stop_id` (`/board/{stopCode}`). |
| Dolasci | Zaslon prikazuje samo polaske (linija, odredište, vrijeme). |
| Jezik | Zaslon na hrvatskom; PDF na hrvatskom ili engleskom, bira se pri preuzimanju. |
| Format PDF-a | A4 ili A3, bira se pri preuzimanju. |
| Stupci PDF-a | Iz stvarnih polazaka po danima u tjednu, ne iz naziva kalendara. |
