-- =====================================================================
-- Terminus: shema baze podataka (PostgreSQL 13+)
--
-- Referentni prikaz sheme koju stvaraju EF Core migracije u
-- Terminus.DAL/Migrations. Datoteka služi kao dokumentacija modela
-- podataka; izvor istine su entiteti, konfiguracije i migracije.
-- Skripta se ne koristi za stvaranje baze.
--
-- Model prati GTFS Static uz vlastite surogat ključeve (uuid). GTFS
-- identifikatori (stop_id, route_id, service_id, trip_id) spremaju se u
-- stupce `code` i jedinstveni su unutar tablice.
--
-- Terminus je single-tenant: svaka tvrtka ima vlastitu instancu i bazu,
-- pa podaci nemaju tenant ključ.
--
-- Konvencije:
--   * nazivi tablica i stupaca u snake_case (EFCore.NamingConventions)
--   * primarni ključevi pk_<tablica>, strani ključevi
--     fk_<tablica>_<referencirana_tablica>_<stupac>
--   * jedinstvenost uq_<tablica>_<stupci>, indeksi ix_<tablica>_<stupci>,
--     CHECK ograničenja ck_<tablica>_<pravilo>
--   * created_at postavlja baza pri unosu; updated_at održava aplikacija
--     (SaveChanges interceptor)
-- =====================================================================

-- ---------------------------------------------------------------------
-- agencies: prijevoznici (operateri linija). Ako je tablica prazna, prvi
-- se stvara pri pokretanju iz konfiguracije (Agency:*). GTFS: agency.txt
-- ---------------------------------------------------------------------
CREATE TABLE agencies (
    id          uuid        NOT NULL DEFAULT gen_random_uuid(),
    name        text        NOT NULL,
    url         text        NOT NULL,                       -- GTFS agency_url (obavezan)
    timezone    text        NOT NULL DEFAULT 'Europe/Zagreb', -- IANA naziv zone
    lang        text        NOT NULL DEFAULT 'hr',          -- BCP 47 oznaka jezika
    phone       text,
    email       text,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_agencies PRIMARY KEY (id)
);

-- ---------------------------------------------------------------------
-- stops: stajališta, perone i stanice (kolodvore). GTFS: stops.txt
--   location_type:       0 = stajalište/peron, 1 = stanica (roditelj perona)
--   wheelchair_boarding: 0 = nepoznato, 1 = pristupačno, 2 = nepristupačno
-- ---------------------------------------------------------------------
CREATE TABLE stops (
    id                  uuid             NOT NULL DEFAULT gen_random_uuid(),
    code                text             NOT NULL,           -- GTFS stop_id
    name                text             NOT NULL,
    description         text,
    latitude            double precision NOT NULL,
    longitude           double precision NOT NULL,
    location_type       smallint         NOT NULL DEFAULT 0,
    parent_station_id   uuid,
    platform_code       text,                                -- oznaka perona, npr. "3" ili "A"
    wheelchair_boarding smallint         NOT NULL DEFAULT 0,
    is_active           boolean          NOT NULL DEFAULT true,
    created_at          timestamptz      NOT NULL DEFAULT now(),
    updated_at          timestamptz      NOT NULL DEFAULT now(),

    CONSTRAINT pk_stops PRIMARY KEY (id),
    CONSTRAINT fk_stops_stops_parent_station_id
        FOREIGN KEY (parent_station_id) REFERENCES stops (id) ON DELETE SET NULL,
    CONSTRAINT ck_stops_latitude       CHECK (latitude  BETWEEN -90  AND 90),
    CONSTRAINT ck_stops_longitude      CHECK (longitude BETWEEN -180 AND 180),
    CONSTRAINT ck_stops_location_type  CHECK (location_type IN (0, 1)),
    CONSTRAINT ck_stops_wheelchair     CHECK (wheelchair_boarding IN (0, 1, 2)),
    CONSTRAINT ck_stops_not_own_parent CHECK (parent_station_id IS NULL OR parent_station_id <> id)
);

CREATE UNIQUE INDEX uq_stops_code              ON stops (code);
CREATE INDEX        ix_stops_parent_station_id ON stops (parent_station_id);

-- ---------------------------------------------------------------------
-- routes: linije. GTFS: routes.txt
--   agency_id: prijevoznik koji vozi liniju (GTFS routes.agency_id);
--              prijevoznik koji ima linije ne može se obrisati
--   route_type: GTFS vrijednost (3 = autobus)
--   color, text_color: hex bez znaka #, npr. 'FF0000'
-- ---------------------------------------------------------------------
CREATE TABLE routes (
    id          uuid        NOT NULL DEFAULT gen_random_uuid(),
    agency_id   uuid        NOT NULL,
    code        text        NOT NULL,                  -- GTFS route_id
    short_name  text,                                  -- npr. "12"
    long_name   text,                                  -- npr. "Osijek - Đakovo"
    description text,
    route_type  smallint    NOT NULL DEFAULT 3,
    color       char(6),
    text_color  char(6),
    sort_order  integer,                               -- GTFS route_sort_order
    is_active   boolean     NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_routes PRIMARY KEY (id),
    CONSTRAINT fk_routes_agencies_agency_id
        FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE RESTRICT,
    CONSTRAINT ck_routes_name       CHECK (short_name IS NOT NULL OR long_name IS NOT NULL),
    CONSTRAINT ck_routes_color      CHECK (color      IS NULL OR color      ~ '^[0-9A-Fa-f]{6}$'),
    CONSTRAINT ck_routes_text_color CHECK (text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$')
);

CREATE UNIQUE INDEX uq_routes_code      ON routes (code);
CREATE INDEX        ix_routes_agency_id ON routes (agency_id);

-- ---------------------------------------------------------------------
-- service_calendars: tjedni obrazac vožnje unutar razdoblja,
-- npr. "Radni dan", "Subota", "Nedjelja i praznici". GTFS: calendar.txt
-- ---------------------------------------------------------------------
CREATE TABLE service_calendars (
    id          uuid        NOT NULL DEFAULT gen_random_uuid(),
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

    CONSTRAINT pk_service_calendars PRIMARY KEY (id),
    CONSTRAINT ck_service_calendars_dates CHECK (end_date >= start_date)
);

CREATE UNIQUE INDEX uq_service_calendars_code ON service_calendars (code);

-- ---------------------------------------------------------------------
-- calendar_dates: iznimke kalendara za pojedine datume (praznici,
-- dodatni dani vožnje). GTFS: calendar_dates.txt
--   exception_type: 1 = vozi taj dan, 2 = ne vozi taj dan
-- ---------------------------------------------------------------------
CREATE TABLE calendar_dates (
    service_calendar_id uuid     NOT NULL,
    date                date     NOT NULL,
    exception_type      smallint NOT NULL,

    CONSTRAINT pk_calendar_dates PRIMARY KEY (service_calendar_id, date),
    CONSTRAINT fk_calendar_dates_service_calendars_service_calendar_id
        FOREIGN KEY (service_calendar_id) REFERENCES service_calendars (id) ON DELETE CASCADE,
    CONSTRAINT ck_calendar_dates_type CHECK (exception_type IN (1, 2))
);

CREATE INDEX ix_calendar_dates_date ON calendar_dates (date);

-- ---------------------------------------------------------------------
-- trips: pojedinačna vožnja linije, npr. Osijek -> Đakovo u 06:15.
-- GTFS: trips.txt
--
-- service_calendar_id koristi NO ACTION: kalendar koji koriste vožnje ne
-- može se obrisati.
-- ---------------------------------------------------------------------
CREATE TABLE trips (
    id                    uuid        NOT NULL DEFAULT gen_random_uuid(),
    route_id              uuid        NOT NULL,
    service_calendar_id   uuid        NOT NULL,
    code                  text        NOT NULL,         -- GTFS trip_id
    headsign              text,                         -- natpis smjera na vozilu
    short_name            text,
    direction_id          smallint,                     -- 0 ili 1 (smjer tamo/natrag)
    block_id              text,                         -- vožnje istog vozila
    wheelchair_accessible smallint    NOT NULL DEFAULT 0,
    bikes_allowed         smallint    NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_trips PRIMARY KEY (id),
    CONSTRAINT fk_trips_routes_route_id
        FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE,
    CONSTRAINT fk_trips_service_calendars_service_calendar_id
        FOREIGN KEY (service_calendar_id) REFERENCES service_calendars (id) ON DELETE NO ACTION,
    CONSTRAINT ck_trips_direction  CHECK (direction_id IS NULL OR direction_id IN (0, 1)),
    CONSTRAINT ck_trips_wheelchair CHECK (wheelchair_accessible IN (0, 1, 2)),
    CONSTRAINT ck_trips_bikes      CHECK (bikes_allowed IN (0, 1, 2))
);

CREATE UNIQUE INDEX uq_trips_code                 ON trips (code);
CREATE INDEX        ix_trips_route_id             ON trips (route_id);
CREATE INDEX        ix_trips_service_calendar_id  ON trips (service_calendar_id);

-- ---------------------------------------------------------------------
-- stop_times: dolazak i polazak vožnje na svakoj stanici. GTFS: stop_times.txt
--
-- Vremena su u sekundama od ponoći dana vožnje. GTFS dopušta vrijednosti
-- iznad 24 h za vožnje koje prelaze ponoć (25:30:00 = 91800); gornja
-- granica je 48 h.
--   pickup_type, drop_off_type: 0 = redovno, 1 = nema, 2 = nazvati
--                               prijevoznika, 3 = dogovor s vozačem
--   timepoint: true = točno vrijeme, false = okvirno
--
-- stop_id koristi NO ACTION: stanica koja se koristi u voznom redu ne može
-- se obrisati.
-- ---------------------------------------------------------------------
CREATE TABLE stop_times (
    trip_id         uuid     NOT NULL,
    stop_sequence   integer  NOT NULL,                 -- redni broj na vožnji, raste
    stop_id         uuid     NOT NULL,
    arrival_time    integer  NOT NULL,
    departure_time  integer  NOT NULL,
    stop_headsign   text,
    pickup_type     smallint NOT NULL DEFAULT 0,
    drop_off_type   smallint NOT NULL DEFAULT 0,
    timepoint       boolean  NOT NULL DEFAULT true,

    CONSTRAINT pk_stop_times PRIMARY KEY (trip_id, stop_sequence),
    CONSTRAINT fk_stop_times_trips_trip_id
        FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE,
    CONSTRAINT fk_stop_times_stops_stop_id
        FOREIGN KEY (stop_id) REFERENCES stops (id) ON DELETE NO ACTION,
    CONSTRAINT ck_stop_times_sequence  CHECK (stop_sequence >= 0),
    CONSTRAINT ck_stop_times_arrival   CHECK (arrival_time   BETWEEN 0 AND 172800),
    CONSTRAINT ck_stop_times_departure CHECK (departure_time BETWEEN 0 AND 172800),
    CONSTRAINT ck_stop_times_order     CHECK (departure_time >= arrival_time),
    CONSTRAINT ck_stop_times_pickup    CHECK (pickup_type   IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_dropoff   CHECK (drop_off_type IN (0, 1, 2, 3))
);

-- Polasci sa stanice poredani po vremenu (vozni red stanice, prikaz polazaka).
-- Pokriva i strani ključ stop_id.
CREATE INDEX ix_stop_times_stop_departure ON stop_times (stop_id, departure_time);
