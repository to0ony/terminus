# Arhitektura

Terminus je ASP.NET Core Web API (.NET 10) nad PostgreSQL bazom, organiziran
u slojeve. Svaki sloj ima zaseban projekt sa sučeljima (`*.Common`) i projekt
s implementacijom.

## Projekti

| Projekt | Odgovornost |
|---|---|
| `Terminus.Common` | Tipovi zajednički svim slojevima: `PagedResult<T>`, iznimke (`NotFoundException`, `ConflictException`, `ValidationException`, `UnprocessableException`) i GTFS formati (`GtfsTime`, `GtfsDate`). Bez ovisnosti. |
| `Terminus.Model.Common` | Sučelja domenskih modela (`IStopModel` …), enumi (`LocationType`, `RouteType` …) i filteri za pretragu. |
| `Terminus.Model` | Implementacije domenskih modela. |
| `Terminus.DAL` | EF Core: entiteti, konfiguracije, `TerminusDbContext`, interceptori i migracije. |
| `Terminus.Repository.Common` | Sučelja repozitorija. |
| `Terminus.Repository` | Pristup podacima preko EF Corea; mapiranje entitet ↔ model i prevođenje grešaka baze u iznimke iz `Common`. |
| `Terminus.Service.Common` | Sučelja servisa. |
| `Terminus.Service` | Poslovna pravila i validacija. |
| `Terminus.WebApi` | Kontroleri, request/response DTO-i, obrada grešaka, DI registracija. |
| `Terminus.Tests` | Testovi (u pripremi). |

## Ovisnosti

| Projekt | Referencira |
|---|---|
| `Common` | — |
| `Model.Common` | Common |
| `Model` | Model.Common, Common |
| `DAL` | Common |
| `Repository.Common` | Model.Common, Common |
| `Repository` | Repository.Common, DAL, Model, Model.Common, Common |
| `Service.Common` | Model.Common, Common |
| `Service` | Service.Common, Repository.Common, Model, Model.Common, Common |
| `WebApi` | Service.Common, Service, Repository.Common, Repository, DAL, Model, Common |

- Sučelja (`*.Common`) ovise samo o `Model.Common` i `Common`, nikad o EF Coreu.
- Servisi znaju samo za sučelja repozitorija; EF Core i Npgsql vidi samo `Repository` (i `DAL`).
- WebApi referencira implementacije isključivo radi registracije u DI-ju.

## Tok zahtjeva

```
HTTP ─► Controller ─► Service ─► Repository ─► DbContext ─► PostgreSQL
         DTO ↔ Model   pravila    Model ↔ Entitet
```

1. **Controller** prima request DTO, pretvara ga u model i poziva servis. Ne sadrži logiku.
2. **Service** normalizira ulaz (trim, prazno → `null`), provjerava poslovna pravila
   i baca `ValidationException` sa svim greškama odjednom.
3. **Repository** izvršava upit, mapira entitet u model i prevodi poznata kršenja
   ograničenja (prema imenu ograničenja) u `ConflictException` ili `NotFoundException`.
4. **`TerminusExceptionHandler`** pretvara iznimke iz `Common` u
   [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) odgovore.

| Iznimka | HTTP |
|---|---|
| `ValidationException` | 400 (s greškama po polju) |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `UnprocessableException` | 422 (s popisom problema, npr. kod GTFS izvoza) |
| ostalo | 500 |

## Jedna tvrtka po instanci

Terminus je single-tenant: svaka tvrtka dobiva vlastitu instancu i bazu, pa
podaci nemaju tenant ključ i upiti ne filtriraju po tvrtki. URL-ovi su
`/api/stops`, `/api/routes`, `/api/calendars`, `/api/trips` i `/api/gtfs`.

`agencies` su GTFS prijevoznici (operateri). Instanca ih može imati više, a
jedina veza s njima je `routes.agency_id`: linija mora imati postojećeg
prijevoznika, a prijevoznik koji ima linije ne može se obrisati (409).

Ako je tablica `agencies` prazna, `AgencySeeder` pri pokretanju stvara
prijevoznika iz konfiguracije (`Agency:Name`, `Agency:Url`, `Agency:Timezone`,
`Agency:Lang`). Nedostajuća ili neispravna konfiguracija i nedostupna baza samo
se zapisuju u log; postojeći prijevoznici se nikad ne mijenjaju.

## Baza podataka

- Shema se vodi **code-first** kroz EF Core migracije u `Terminus.DAL/Migrations`.
  [`terminus_schema.sql`](terminus_schema.sql) je referentni prikaz sheme.
- Nazivi su u snake_case (`EFCore.NamingConventions`); ključevi, indeksi i CHECK
  ograničenja imaju eksplicitna imena koja repozitoriji koriste za prevođenje grešaka.
- Primarni ključevi su `uuid` koje generira baza (`gen_random_uuid()`).
- `created_at` postavlja baza; `updated_at` postavlja `AuditableInterceptor`
  za entitete koji implementiraju `IAuditable`.
- Više o modelu i vezi s GTFS-om: [data-model.md](data-model.md).

## Konvencije u kodu

- Svaki modul (Stops, Routes …) prolazi kroz sve slojeve istim obrascem:
  sučelje modela → model → repozitorij → servis → DTO-i → kontroler.
- Mapiranje je ručno (statičke klase `*Mapper` u Repositoryju, `*Mappings` u WebApi-ju).
- Popisi su uvijek straničeni (`page`, `pageSize` ≤ 100) i vraćaju `PagedResult<T>`.
- Sve metode su asinkrone i primaju `CancellationToken`.
- PUT zamjenjuje cijeli resurs; djelomične izmjene (PATCH) nisu podržane.
