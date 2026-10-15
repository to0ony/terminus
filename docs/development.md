# Lokalni razvoj

## Preduvjeti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 13 ili noviji
- EF Core CLI: `dotnet tool install --global dotnet-ef`

Sve naredbe u nastavku pokreću se iz mape `backend/` (`cd backend`), gdje su
`Terminus.slnx` i `global.json`.

## Baza i konfiguracija

Aplikacija čita connection string `ConnectionStrings:Default`. U razvoju se
drži u [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets),
izvan repozitorija:

```sh
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=terminus;Username=<korisnik>;Password=<lozinka>" \
  --project Terminus.WebApi
```

Bez connection stringa aplikacija se ne pokreće.

Prvi prijevoznik stvara se pri pokretanju iz sekcije `Agency` ako je tablica
`agencies` prazna (vidi [README](../README.md#konfiguracija-instance)). Lokalno
se može postaviti isto kroz User Secrets:

```sh
dotnet user-secrets set "Agency:Name" "Test prijevoznik" --project Terminus.WebApi
dotnet user-secrets set "Agency:Url" "https://example.com" --project Terminus.WebApi
```

Stvaranje sheme u praznoj bazi:

```sh
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi
```

## Pokretanje

```sh
dotnet run --project Terminus.WebApi --launch-profile http
```

API je dostupan na `http://localhost:5183`. U okruženju `Development` OpenAPI
opis je na `/openapi/v1.json`; može se uvesti u Bruno ili Postman.
Primjeri zahtjeva nalaze se u `backend/Terminus.WebApi/Terminus.WebApi.http`.
U Visual Studiju se otvara `backend/Terminus.slnx`.

## Testovi

```sh
dotnet test --solution Terminus.slnx
```

Testovi su u `Terminus.Tests` (xUnit v3, Microsoft Testing Platform; način
pokretanja određuje `global.json`). Unit testovi ne trebaju bazu: servisi
dobivaju lažne repozitorije iz `Terminus.Tests/Fakes`.

GitHub Actions (`.github/workflows/ci.yml`) na svaki push i pull request na
`master` u mapi `backend/` radi restore, build u Release konfiguraciji i
pokreće testove.

## Migracije

Shema se mijenja isključivo kroz migracije:

```sh
# nova migracija nakon izmjene entiteta ili konfiguracije
dotnet ef migrations add <NazivMigracije> --project Terminus.DAL --startup-project Terminus.WebApi

# primjena na lokalnu bazu
dotnet ef database update --project Terminus.DAL --startup-project Terminus.WebApi

# idempotentna SQL skripta za ostala okruženja
dotnet ef migrations script --idempotent -o deploy.sql --project Terminus.DAL --startup-project Terminus.WebApi
```

Prije commita treba pregledati generiranu migraciju. EF ponekad doda
vrijednosti koje nisu poželjne, npr. `defaultValue: ""` kod pretvaranja
stupca u `NOT NULL`. Nakon promjene sheme ažurira se i
[`terminus_schema.sql`](terminus_schema.sql).
