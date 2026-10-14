-- =====================================================================
-- Terminus: shema baze (PostgreSQL 13+)
-- Model prati GTFS Static, ali s vlastitim (surogat) ključevima.
-- GTFS "string ID-ovi" nalaze se u stupcima `code` i koriste se pri izvozu.
--
-- Prije pokretanja (jednom, kao superkorisnik):
--   CREATE DATABASE terminus;
-- Zatim se spoji na bazu `terminus` i pokreni ovu skriptu.
--
-- NAPOMENA: ako shemu vodiš kroz EF Core migracije, NE pokreći ovu skriptu
-- na istoj bazi. Dva izvora istine se razilaze. Za server generiraj SQL iz migracija:
--   dotnet ef migrations script --idempotent -o deploy.sql
--   --project Terminus.DAL --startup-project Terminus.WebApi
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- agencies: prijevoznik, a ujedno i "tenant" (svaka tvrtka vidi samo svoje)
-- ---------------------------------------------------------------------
CREATE TABLE agencies (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        text        NOT NULL,
    url         text,
    timezone    text        NOT NULL DEFAULT 'Europe/Zagreb',  -- IANA naziv
    lang        text        NOT NULL DEFAULT 'hr',
    phone       text,
    email       text,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------
-- stops: stanice, stajališta i perone
-- location_type: 0 = stajalište/peron, 1 = stanica (kolodvor, roditelj perona)
-- ---------------------------------------------------------------------
CREATE TABLE stops (
    id                  uuid             PRIMARY KEY DEFAULT gen_random_uuid(),
    agency_id           uuid             NOT NULL REFERENCES agencies (id) ON DELETE CASCADE,
    code                text             NOT NULL,           -- GTFS stop_id
    name                text             NOT NULL,
    description         text,
    latitude            double precision NOT NULL,
    longitude           double precision NOT NULL,
    location_type       smallint         NOT NULL DEFAULT 0,
    parent_station_id   uuid             REFERENCES stops (id) ON DELETE SET NULL,
    platform_code       text,                                -- npr. "3" ili "A"
    wheelchair_boarding smallint         NOT NULL DEFAULT 0, -- 0 nepoznato, 1 da, 2 ne
    is_active           boolean          NOT NULL DEFAULT true,
    created_at          timestamptz      NOT NULL DEFAULT now(),
    updated_at          timestamptz      NOT NULL DEFAULT now(),

    CONSTRAINT uq_stops_agency_code UNIQUE (agency_id, code),
    CONSTRAINT ck_stops_latitude    CHECK (latitude  BETWEEN -90  AND 90),
    CONSTRAINT ck_stops_longitude   CHECK (longitude BETWEEN -180 AND 180),
    CONSTRAINT ck_stops_location_type CHECK (location_type IN (0, 1)),
    CONSTRAINT ck_stops_wheelchair  CHECK (wheelchair_boarding IN (0, 1, 2)),
    CONSTRAINT ck_stops_not_own_parent CHECK (parent_station_id IS NULL OR parent_station_id <> id)
);

CREATE INDEX ix_stops_agency_id         ON stops (agency_id);
CREATE INDEX ix_stops_parent_station_id ON stops (parent_station_id);

-- ---------------------------------------------------------------------
-- routes: linije
-- route_type: 3 = autobus (GTFS vrijednost)
-- ---------------------------------------------------------------------
CREATE TABLE routes (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    agency_id   uuid        NOT NULL REFERENCES agencies (id) ON DELETE CASCADE,
    code        text        NOT NULL,                  -- GTFS route_id
    short_name  text,                                  -- npr. "12"
    long_name   text,                                  -- npr. "Osijek - Đakovo"
    description text,
    route_type  smallint    NOT NULL DEFAULT 3,
    color       char(6),                               -- hex bez #, npr. 'FF0000'
    text_color  char(6),
    sort_order  integer,
    is_active   boolean     NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_routes_agency_code UNIQUE (agency_id, code),
    CONSTRAINT ck_routes_name        CHECK (short_name IS NOT NULL OR long_name IS NOT NULL),
    CONSTRAINT ck_routes_color       CHECK (color      IS NULL OR color      ~ '^[0-9A-Fa-f]{6}$'),
    CONSTRAINT ck_routes_text_color  CHECK (text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$')
);

CREATE INDEX ix_routes_agency_id ON routes (agency_id);

-- ---------------------------------------------------------------------
-- service_calendars: kada se vozi (GTFS calendar.txt)
-- npr. "Radni dan", "Subota", "Nedjelja i praznici", "Školski dani"
-- ---------------------------------------------------------------------
CREATE TABLE service_calendars (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    agency_id   uuid        NOT NULL REFERENCES agencies (id) ON DELETE CASCADE,
    code        text        NOT NULL,                  -- GTFS service_id
    name        text        NOT NULL,
    monday      boolean     NOT NULL DEFAULT false,
    tuesday     boolean     NOT NULL DEFAULT false,
    wednesday   boolean     NOT NULL DEFAULT false,
    thursday    boolean     NOT NULL DEFAULT false,
    friday      boolean     NOT NULL DEFAULT false,
    saturday    boolean     NOT NULL DEFAULT false,
    sunday      boolean     NOT NULL DEFAULT false,
    start_date  date        NOT NULL,
    end_date    date        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_service_calendars_agency_code UNIQUE (agency_id, code),
    CONSTRAINT ck_service_calendars_dates       CHECK (end_date >= start_date)
);

CREATE INDEX ix_service_calendars_agency_id ON service_calendars (agency_id);

-- ---------------------------------------------------------------------
-- calendar_dates: iznimke (praznici, dodatni dani) (GTFS calendar_dates.txt)
-- exception_type: 1 = vozi taj dan, 2 = ne vozi taj dan
-- ---------------------------------------------------------------------
CREATE TABLE calendar_dates (
    service_calendar_id uuid     NOT NULL REFERENCES service_calendars (id) ON DELETE CASCADE,
    date                date     NOT NULL,
    exception_type      smallint NOT NULL,

    PRIMARY KEY (service_calendar_id, date),
    CONSTRAINT ck_calendar_dates_type CHECK (exception_type IN (1, 2))
);

CREATE INDEX ix_calendar_dates_date ON calendar_dates (date);

-- ---------------------------------------------------------------------
-- trips: jedna konkretna vožnja (npr. Osijek -> Đakovo u 06:15)
-- agency_id je ovdje namjerno redundantan (brži tenant filter i
-- jedinstvenost GTFS trip_id u cijelom feedu). Servis pazi da route,
-- calendar i agency pripadaju istoj tvrtki.
-- ---------------------------------------------------------------------
CREATE TABLE trips (
    id                   uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    agency_id            uuid        NOT NULL REFERENCES agencies (id) ON DELETE CASCADE,
    route_id             uuid        NOT NULL REFERENCES routes (id) ON DELETE CASCADE,
    service_calendar_id  uuid        NOT NULL REFERENCES service_calendars (id),
    code                 text        NOT NULL,         -- GTFS trip_id
    headsign             text,                         -- natpis na autobusu / smjer
    short_name           text,
    direction_id         smallint,                     -- 0 ili 1 (tamo/natrag)
    block_id             text,                         -- isti autobus vozi više vožnji
    wheelchair_accessible smallint   NOT NULL DEFAULT 0,
    bikes_allowed        smallint    NOT NULL DEFAULT 0,
    created_at           timestamptz NOT NULL DEFAULT now(),
    updated_at           timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_trips_agency_code      UNIQUE (agency_id, code),
    CONSTRAINT ck_trips_direction        CHECK (direction_id IS NULL OR direction_id IN (0, 1)),
    CONSTRAINT ck_trips_wheelchair       CHECK (wheelchair_accessible IN (0, 1, 2)),
    CONSTRAINT ck_trips_bikes            CHECK (bikes_allowed IN (0, 1, 2))
);

CREATE INDEX ix_trips_agency_id          ON trips (agency_id);
CREATE INDEX ix_trips_route_id           ON trips (route_id);
CREATE INDEX ix_trips_service_calendar_id ON trips (service_calendar_id);

-- ---------------------------------------------------------------------
-- stop_times: vrijeme dolaska/odlaska vožnje na svakoj stanici
-- Vremena su SEKUNDE OD PONOĆI (GTFS dopušta > 24h, npr. 25:30:00 = 91800).
-- ---------------------------------------------------------------------
CREATE TABLE stop_times (
    trip_id         uuid     NOT NULL REFERENCES trips (id) ON DELETE CASCADE,
    stop_sequence   integer  NOT NULL,                 -- redni broj na vožnji, raste
    stop_id         uuid     NOT NULL REFERENCES stops (id),
    arrival_time    integer  NOT NULL,                 -- sekunde od ponoći
    departure_time  integer  NOT NULL,
    stop_headsign   text,
    pickup_type     smallint NOT NULL DEFAULT 0,      -- 0 redovno, 1 nema ukrcaja, 2 nazvati, 3 dogovor s vozačem
    drop_off_type   smallint NOT NULL DEFAULT 0,
    timepoint       boolean  NOT NULL DEFAULT true,   -- true = vrijeme je točno, false = okvirno

    PRIMARY KEY (trip_id, stop_sequence),
    CONSTRAINT ck_stop_times_sequence  CHECK (stop_sequence >= 0),
    CONSTRAINT ck_stop_times_arrival   CHECK (arrival_time   BETWEEN 0 AND 172800),  -- max 48h
    CONSTRAINT ck_stop_times_departure CHECK (departure_time BETWEEN 0 AND 172800),
    CONSTRAINT ck_stop_times_order     CHECK (departure_time >= arrival_time),
    CONSTRAINT ck_stop_times_pickup    CHECK (pickup_type   IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_dropoff   CHECK (drop_off_type IN (0, 1, 2, 3))
);

-- Najčešći upit: "polasci sa stanice X poredani po vremenu" (PDF + live view)
CREATE INDEX ix_stop_times_stop_departure ON stop_times (stop_id, departure_time);

COMMIT;
