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
| `/routes/:id/variants/:variantId` | **editor varijante** na karti (`new`, `new?from=` za kopiju, `new?reverse=` za povratni smjer) (opis niže) |
| `/routes/:id/schedule` | **grafikon voznog reda** linije s uređivanjem vožnji (opis niže) |
| `/stops`, `/stops/:id` | stanice s kartom, **nova stanica klikom** i **prijedlozi iz OpenStreetMapa**; stanica s izmjenom, brisanjem, peronima kolodvora, linijama, polascima za dan, premještanjem na karti, **PDF voznim redom** i poveznicom na zaslon |
| `/calendars` | sezone voznog reda (i nova sezona) te ostali kalendari, npr. uvezeni iz GTFS-a |
| `/seasons/:id` | sezona: tipovi dana, pravilo za praznike i svi mjeseci; klik na dan mijenja kako vozi (kao drugi tip dana, ne vozi) uz napomenu; **razdoblja** (npr. školski raspust: radni dan vozi kao radni dan za vrijeme raspusta); **kopiranje sezone** za sljedeću godinu (zadano isti datumi godinu kasnije, godine u nazivu +1), po želji s vožnjama |
| `/calendars/:id` | kalendar s danima u tjednu i iznimkama na mjesečnom prikazu |
| `/agencies` | prijevoznici s brojem linija; **novi prijevoznik**, izmjena i brisanje (opis niže) |
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

## Stanice

Instanca koja kreće od nule stanice radi na stranici Stanice:

- **Tražilica mjesta** gore lijevo na karti (geokoder
  [Photon](https://photon.komoot.io), podaci OpenStreetMapa, bez ključa)
  pomiče kartu na grad ili adresu.
- **„Nova stanica”** uključuje postavljanje: klik na kartu otvara dijalog
  stanice na tom mjestu, a postavljanje ostaje uključeno za sljedeću. Klik
  unutar 150 m od upravo dodane predlaže isti naziv, za drugi peron.
- **„Iz OpenStreetMapa”** učitava stajališta autobusa, tramvaja i vlaka u
  vidljivom dijelu karte (javni [Overpass API](https://overpass-api.de),
  područje do oko 25 km). Stajališta bez naziva i ona unutar 20 m od postojeće
  stanice se ne nude. Klik odabire sivu točku, a odabrane postaju stanice s
  nazivom, peronom (`local_ref`), oznakom za putnike (`ref`) i pristupačnošću
  iz OSM-a (`features/stops/osm.ts`).

Dijalog stanice (nova i izmjena) ima naziv, peron, oznaku za putnike, vrstu
(stajalište ili kolodvor), kolodvor kojem pripada, pristupačnost i GTFS ID.
GTFS ID nove stanice je naziv i peron bez dijakritike (`glavni-kolodvor-a`), a
ako je zauzet dodaje se broj (`-2`, `-3`). Kolodvor na svojoj stranici popisuje
perone i nudi „Dodaj peron” (na mjestu kolodvora, pa se premjesti). Stanica
koju koriste vožnje ili varijante ne može se obrisati; nju se označi
neaktivnom.

## Prijevoznici

Na stranici Prijevoznici dodaju se, uređuju i brišu prijevoznici (GTFS
`agency`). Forma je ista kao u čarobnjaku prvog pokretanja
(`features/agencies/AgencyForm.tsx`): GTFS oznaka novog prijevoznika bez
upisa izvodi se iz naziva, a vremenska zona je ista kao kod postojećih jer je
GTFS traži istu za cijeli feed.

**Više prijevoznika (konzorcij).** Kad gradski prijevoz vozi više tvrtki,
svaka je zaseban prijevoznik, a svaka linija ima svog (izbor u dijalogu linije
pojavi se čim postoje barem dva). GTFS izvoz navodi sve prijevoznike, popis
linija dobiva filter i stupac Prijevoznik, a zaslon stanice i PDF prikazuju
naziv iz Postavki, pa putnik vidi jedan naziv sustava. Dok taj naziv nije
upisan, a prijevoznika je više, stranica Prijevoznici upozorava na to.

**Brisanje.** Prijevoznik bez linija briše se odmah. Prijevoznik s linijama
briše se tek kad se odabere tko ih preuzima: linije i cijene karata prelaze
na tog prijevoznika, a vožnje i vozni red ostaju. Zadnji prijevoznik se ne
može obrisati.

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
- Stanice se premještaju povlačenjem za ručku (ili strelicama gore i dolje) i
  uklanjaju; varijanta je na karti numerirana crta, a isti brojevi su u popisu
  stanica i dionica trase. Naziv je zadano „prva → zadnja stanica”.
- Stupac **km** su kilometri od prve stanice, kao u registriranom voznom redu:
  po trasi, a gdje trase (još) nema zračnom crtom, uz oznaku ≈. Dionice trase
  imaju i svoju duljinu.
- Varijanta koju koriste vožnje zadržava stanice, a mijenjaju se samo vremena
  (za drugi redoslijed kopija).
  Kad su vremena promijenjena, spremanje pita „Samo varijanta” ili „Presnimi
  sve vožnje”: sve vožnje varijante, u svim tipovima dana, dobivaju vremena
  varijante uz isti polazak s prve stanice (i ručno dotjerane; sitne izmjene
  rade se u grafikonu). Vožnja koja bi završila iza 48:00 se preskače.
- **Povratni smjer** (gumb uz varijantu na stranici linije) otvara novu
  varijantu suprotnog smjera: stanice su obrnutim redom, a svaka je zamijenjena
  najbližom aktivnom stanicom istog naziva do 400 m koja nije u polaznom smjeru
  (stajalište s druge strane ceste ili drugi peron). Stanice bez para (npr.
  okretište) ostaju iste i označene su za provjeru. Vremena vožnje dionica
  ostaju ista, samo u suprotnom smjeru (`reverse.ts`).
  Ako polazna varijanta ima vožnji, gumb pita „Samo stanice” ili „Stanice i
  vožnje”; uz vožnje nakon spremanja varijante otvara se grafikon s
  preslikavanjem (niže).

**Trasa** (prekidač „Stanice / Trasa” u editoru varijante) prati ceste
(autobus, trolejbus) ili pruge (tramvaj, vlak, metro) iz OpenStreetMapa, bez
rutera:

- Ceste ili pruge u koridoru oko stanica (300 m, šire za duge međugradske
  dionice) dolaze iz vektorskih pločica OpenFreeMapa na zoomu 14, istih iz
  kojih se crta podloga (`tileNetwork.ts`): par desetaka pločica s CDN-a za
  manje od sekunde. Ulice koje dijele točku pločice su spojene, a kraj
  sporedne ulice uz glavnu (pločice su pojednostavljene) umeće se u nju.
  Najkraći put od stanice do stanice računa se u pregledniku (`osmNetwork.ts`).
  Javni Overpass se za ovo ne koristi: za koridor cijele linije trebalo mu je
  i pola minute, a često nije odgovarao. Poštuju se jednosmjerne ulice
  (i kružni tokovi), osim gdje je autobusu dopušteno (`oneway:bus=no`);
  privatne ceste i parkirališta ne ulaze u mrežu. Stanica se spaja na najbližu
  ulicu, a servisni put ima prednost samo ako je bitno bliži.
- Trasa je po dionicama između uzastopnih stanica (`variantShape.ts`,
  `useVariantShape.ts`). Povlačenje crte dodaje međutočku i ponovno računa
  samo tu dionicu, povlačenje međutočke je pomiče, a desni klik je briše.
  Dionica bez puta (rupa u OSM-u) je crvena ravna crta; ručna dionica je ravna
  crta kroz međutočke. Ako put nije nađen, dohvaća se širi koridor te dionice.
- Spremljena trasa se pri otvaranju dijeli po stanicama (najbliža točka trase
  redom), pa nakon dodane ili premještene stanice nastaju samo nove dionice, a
  popravci ostalih ostaju; nove se izračunaju pri ulasku u trasu ili spremanju.
- Trasa se sprema s varijantom kao nova GTFS trasa (`POST /api/shapes`) i
  prelazi na sve vožnje varijante. Kopija varijante preuzima trasu izvorne, a
  povratni smjer je računa iznova.

## Grafikon voznog reda

`features/schedule` je editor vožnji jedne linije u obliku grafikona
(Mareyev dijagram, kao željeznički grafikon prometa):

- **Redci** su stanice smjera: spojeni redoslijed varijanti i vožnji, isti za
  sve tipove dana. Traka varijanti (s brojem vožnji) skriva ili prikazuje
  varijantu, a redci prate prikazane. Razmak između stanica je srednje vrijeme vožnje, pa je
  pravilna vožnja ravna crta, a sporiji dio se vidi kao lom.
- **Vodoravno je vrijeme** dana voznog reda, s ponoći označenom crtkano
  (vožnje iza nje su GTFS vremena 24:xx). Traka gore pokazuje gustoću polazaka
  cijelog dana i prozor koji se vidi. Prozor se povlači po traci, a rubovi
  mu se hvataju i razvlače na raspon koji treba (najmanje 20 minuta);
  kotačić zumira, a Shift+kotačić pomiče.
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
| „Polazak vozi u” | kvačice tipova dana za odabranu vožnju (ili više njih): polazak, npr. „svaki dan”, je jedna vožnja po tipu dana s istim vremenima. Pri spremanju server vožnje u označenim tipovima dana dodaje ili usklađuje (vremena, varijanta, trasa), a iz isključenih briše; izmjena vremena u bilo kojem tipu dana prenosi se na ostale. „Odvoji” izdvaja vožnju (npr. subotom kreće kasnije), a ostale ostaju |
| „Preslikaj iz suprotnog smjera” | povratak za svaki polazak varijante suprotnog smjera, po odabranoj varijanti ovog smjera: okret na terminusu (dolazak + N min) ili pomak od polaska. Okret je zajednički, a pojedini polazak se može dotjerati, pa se zaključa 🔒 i promjena zajedničkog okreta ga ne dira (klik na lokot ga vraća). Polazak u više tipova dana daje jedan povratak u svima njima, s istim vozilom (BlockId). Povratak koji već postoji (±2 min) je isključen, a „Zamijeni postojeće” prvo briše vožnje odabrane varijante. Okret i zaključani polasci pamte se u pregledniku za taj par varijanti (`mirror.ts`, `MirrorDialog`) |
| „Kopiraj u tip dana” | odabrane vožnje ili sve vožnje tipa dana dobivaju kopiju s istim vremenima u drugom tipu dana, i druge sezone (npr. subota = radni dan bez jutarnjeg vrha: kopiraj sve, pa u suboti obriši višak); kopije se spremaju s ostalim izmjenama i uređuju u tom tipu dana |

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
