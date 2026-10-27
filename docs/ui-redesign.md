# UI redesign i analize kvalitete prijevoza

Prijedlozi iz brainstorminga: promjene sučelja dashboarda i editora te
analize GTFS podataka koje bi Terminus mogao nuditi kao alat. Gotovo je
označeno ✅ (branch `feat/ui-redesign`), a ono što je već postojalo prije
prijedloga 〰️; redoslijed je na kraju svakog dijela.

## Trenutno stanje sučelja

Sučelje je gotovo u potpunosti od Mantine komponenti; ručno je samo ono za što
Mantine nema rješenje.

- **Mantine** – kostur je `AppShell` s `NavLink` izbornikom koji se može
  smanjiti na ikone; stranice su od `Paper`, `Table`, `Select`,
  `SegmentedControl`, `Modal`, `Menu`, `Badge`, `Stepper` i sl. Datumi,
  obavijesti, modali i dropzone su iz `@mantine/*`, ikone su Tabler.
- **Ručno** – grafikon voznog reda (`ScheduleChart`) crta se na `<canvas>`,
  karte su MapLibre s vlastitim slojevima, a javne stranice (`board`, `live`)
  imaju vlastite CSS module.
- **Tema** – `frontend/src/app/theme.ts` je zadana Mantine tema
  (`primaryColor: 'blue'`), pa dashboard izgleda kao generički Mantine admin.
  Boje instance iz postavki zasad se ne primjenjuju na dashboard.

## Promjene sučelja

### Vizualni identitet

- **Vlastita tema** – boje instance iz postavki primijenjene i na dashboard,
  font (npr. Inter ili IBM Plex Sans), `tabular-nums` za vremena polazaka,
  ujednačene sjene, obrubi i radijusi komponenti kroz `theme.components`.
- **Boje linija svugdje** – `RouteBadge` već postoji; boja linije neka se
  provlači kroz tablice, karte i grafikon vožnji.
- **Tamna tema** – postoji; provjeriti canvas grafikon i podloge karte u
  tamnom načinu.

### Navigacija i orijentacija

- ✅ **Breadcrumbs** na dubokim stranicama (Linije → 4A → Varijanta → Vozni red).
  Zamijenili su poveznice „← Natrag” (`components/PageBreadcrumbs.tsx`).
- **Command palette (Ctrl+K)** preko `@mantine/spotlight` – skok na liniju ili
  stanicu po broju/imenu i akcije ("Nova stanica", "Izvoz GTFS-a").
- ✅ **Karta uživo u izborniku** – stavka odmah ispod Pregleda; gumb s
  pregleda je uklonjen.
- ✅ **Prečaci u editorima** – grafikon: Ctrl+S, ↑↓ vožnja, Ctrl+A, N crtanje,
  + − 0 zum; editor varijante: Ctrl+S, ↑↓ odabir, Alt+↑↓ premještanje, Del,
  `/` traži, F na karti, Esc. Popis u uputama grafikona i ispod stanica
  (`lib/keyboard.ts`).

### Učitavanje, prazna stanja i greške

- ✅ **Skeletoni** umjesto `PageLoader` spinnera, da stranica manje skače
  (`components/Skeletons.tsx`); spinner ostaje za prijavu, provjeru
  postavljanja, javnu kartu i dijaloge.
- **Prazna stanja** s porukom i akcijom ("Nema linija – uvezi GTFS ili dodaj
  prvu liniju").
- ✅ **Optimistična ažuriranja i poništavanje** iz obavijesti – brisanje
  varijante i razdoblja sezone: potvrda, pa stavka odmah nestaje i 6 s se može
  poništiti, a server briše tek tada (`lib/undo.tsx`). Izmjena dana sezone
  odmah se vidi i može se poništiti. Stanica, linija, sezona i prijevoznik
  imaju samo potvrdu (server često odbija ili briše više toga).

### Pregled (dashboard)

Pretvoriti ga u radnu ploču:

- upozorenja ("kalendar X istječe za 12 dana", "3 vožnje bez dana važenja"),
- brze akcije i zadnje izmjene,
- KPI kartice (linije, polasci danas, vozni km dnevno),
- mjesto za analize iz drugog dijela ovog dokumenta.

### Tablice

- `@tanstack/react-table` i `@tanstack/react-virtual` već su u ovisnostima, a
  popisi koriste obični Mantine `Table` sa straničenjem. Moguće je dodati
  sortiranje po stupcu, odabir više redaka i grupne akcije (deaktiviraj,
  premjesti na drugog prijevoznika), ljepljivo zaglavlje i virtualizaciju
  umjesto straničenja.
- ✅ Prekidač gustoće prikaza (zbijeno / udobno) – u korisničkom izborniku i
  Postavkama, pamti se u pregledniku; mijenja zadani razmak redaka tablica.

### Editori

`VariantEditorPage` i `SchedulePage` s grafikonom su srce aplikacije:

- 〰️ **Undo / redo** povijest izmjena – već postoji u grafikonu (Ctrl+Z /
  Ctrl+Y); u editoru varijante još ne.
- 〰️ **Nespremljene promjene** – upozorenje pri izlasku već postoji u oba
  editora; vidljiv pokazatelj samo u grafikonu (broj izmjena na „Spremi”).
- **Bočni panel umjesto modala** kad se uređuje vožnja, da grafikon ostane
  vidljiv.
- **Tablični unos voznog reda** kao u proračunskoj tablici – Tab/Enter za
  kretanje, lijepljenje iz Excela (prijevoznici vozni red često imaju u
  Excelu).
- **Vođeni koraci za novog korisnika** – checklist "stanice → linija →
  kalendar → vožnje → izvoz" s napretkom.

### Mobitel i javne stranice

- Dashboard na mobitelu ne mora biti potpun za editore, ali javne stranice
  (zaslon stanice, karta uživo) zaslužuju najviše pažnje: PWA instalacija,
  omiljene stanice u pregledniku, visoki kontrast za zaslone na stanicama.

### Pristupačnost

- Fokus i navigacija tipkovnicom na canvas grafikonu i kartama.
- Kontrast boja linija na bijeloj podlozi.
- `aria-live` za odbrojavanje polazaka na javnom zaslonu.

### Redoslijed

1. Vlastita tema i boje instance – mijenja dojam cijele aplikacije.
2. ✅ Breadcrumbs i karta uživo u izborniku; Ctrl+K još nije.
3. Pregled kao radna ploča s upozorenjima.
4. ✅ Skeletoni i poništavanje iz obavijesti; prazna stanja još nisu.
5. Undo i tablični unos u editoru voznog reda – najveći posao, najveća
   vrijednost.

## Analize kvalitete prijevoza

Terminus već ima sve što većini analiza treba (stanice s koordinatama,
varijante s vremenima, kalendari s iznimkama, vožnje), pa se većina može
računati bez vanjskih podataka.

### 1. Ponuda usluge (samo GTFS)

- **Frekvencija i interval** – prosječni i najveći razmak polazaka po liniji,
  stanici i satu; toplinska karta "sat × dan" pokazuje rupe u usluzi.
- **Raspon usluge** – prvi i zadnji polazak po danu u tjednu (linije koje
  završavaju u 19 h ili nemaju nedjeljnu uslugu).
- **Pravilnost intervala** – koeficijent varijacije intervala: ravnomjerni
  polasci naspram zbijenih (7:00, 7:05, pa ništa do 8:10).
- **Očekivano čekanje** za putnika koji dođe u nasumično vrijeme.
- **Polasci po datumu kroz sezonu** – graf broja vožnji po danu iz kalendara i
  iznimki; vide se praznici, ljetni red i slučajno prazni dani. Nadovezuje se
  na postojeći pregled do kada vrijedi vozni red.

### 2. Prostorna pokrivenost

- **Zone pješačke dostupnosti** – 300/400/500 m oko stanica; bolja verzija
  preko OSM pješačke mreže.
- **Pokrivenost stanovništva** – udio stanovnika unutar 400 m od stanice;
  treba vanjski sloj (DZS po naseljima ili GHS/WorldPop mreža).
- **Pokrivenost ponderirana uslugom** – indeks tipa PTAL (London): udaljenost
  i frekvencija u jednoj ocjeni po ćeliji karte.
- **Razmak stanica** duž trase – pregusto usporava liniju, prerijetko smanjuje
  pokrivenost.

### 3. Dostupnost i povezanost

- **Izokrone** – "kamo mogu stići za 30 min s ove stanice u 7:30 utorkom";
  treba RAPTOR ili CSA nad vožnjama (za jednu instancu brzo).
- **Matrica putovanja** između ključnih točaka (kolodvor, bolnica, fakultet,
  centar) s vremenom i brojem presjedanja.
- **Kvaliteta presjedanja** na čvorištima – koliko dolazaka ima priključak u
  2–10 min, a koliko ga propušta za minutu; prijevoznik odmah vidi koje
  polaske pomaknuti.
- **Usporedba s autom / biciklom** – omjer vremena putovanja (OSRM).

### 4. Operativna učinkovitost

- **Vozni kilometri i sati** po liniji, danu i sezoni – osnova za troškove i
  izvješća.
- **Komercijalna brzina** po liniji i dobu dana.
- **Potreban broj vozila** – najveći broj istovremenih vožnji, uz vrijeme
  okretanja na terminusu.
- **Paralelne linije** – dionice s više linija i nekoordiniranim polascima.

### 5. Kvaliteta podataka

Proširenje postojećih provjera prije izvoza:

- nerealne brzine između stanica, stanice daleko od trase, duplicirane
  stanice unutar nekoliko metara, vožnje bez dana važenja, kalendari koji
  uskoro istječu, nepopunjen `wheelchair_boarding`;
- ocjena feeda 0–100 na pregledu;
- po izboru pokretanje MobilityData GTFS validatora.

### 6. Usporedba verzija voznog reda

- Razlika između dviju sezona ili verzija: linije s više ili manje polazaka,
  promjene raspona, stanice bez usluge. Korisno za javnost ("što se mijenja od
  1. rujna") i interno odobravanje.

### 7. Pravednost

- Usluga po četvrtima/naseljima – polasci po stanovniku, prigradska naselja
  naspram centra; treba granice naselja (OSM/DZS).

### Izvan opsega zasad

Pouzdanost (točnost, kašnjenja) traži GTFS-Realtime ili AVL podatke, a karta
uživo zasad simulira vozila iz voznog reda.

### Redoslijed

1. **"Zdravlje voznog reda" na pregledu** – toplinska karta polazaka, raspon
   usluge, rupe u intervalima, polasci po datumu. Bez vanjskih podataka.
2. **Karta pokrivenosti** – zone oko stanica obojane prema frekvenciji
   (lagani PTAL); MapLibre već postoji.
3. **Analiza presjedanja** na odabranom čvorištu.
4. **Izokrone / planer putovanja (RAPTOR)** – najveći posao, ali otključava
   matricu putovanja, usporedbu s autom i "kamo mogu stići" na javnoj
   stranici stanice.
5. **Usporedba verzija / sezona.**
