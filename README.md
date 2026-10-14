# Terminus

Terminus je aplikacija za izradu i upravljanje voznim redovima javnog prijevoza
u formatu [GTFS](https://gtfs.org/). Prijevoznici u njoj vode stanice, linije,
kalendare i vožnje te iz njih izvoze GTFS feed.

Planirane dodatne mogućnosti:

- PDF vozni redovi za pojedine stanice
- prikaz nadolazećih polazaka na stanicama

## Tehnologije

ASP.NET Core Web API (.NET 10), Entity Framework Core, PostgreSQL

## Dokumentacija

- [Lokalni razvoj](docs/development.md)
- [Arhitektura](docs/architecture.md)
- [Model podataka i GTFS](docs/data-model.md)
- [Shema baze](docs/terminus_schema.sql)
