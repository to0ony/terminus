-- =====================================================================
-- Terminus: shema baze podataka (PostgreSQL 13+)
--
-- Referentni prikaz sheme koju stvaraju EF Core migracije u
-- Terminus.DAL/Migrations. Datoteka služi kao dokumentacija modela
-- podataka; izvor istine su entiteti, konfiguracije i migracije.
-- Skripta se ne koristi za stvaranje baze.
--
-- Model prati GTFS Static uz vlastite surogat ključeve (uuid). GTFS
-- identifikatori (agency_id, stop_id, route_id, service_id, trip_id,
-- shape_id, fare_id) spremaju se u stupce `code` i jedinstveni su unutar
-- tablice.
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
-- agencies: prijevoznici (operateri linija). Prvog stvara čarobnjak prvog
-- pokretanja (uvoz GTFS-a ili početak od nule). GTFS: agency.txt
-- ---------------------------------------------------------------------
CREATE TABLE agencies (
    id          uuid        NOT NULL DEFAULT gen_random_uuid(),
    code        text        NOT NULL,                       -- GTFS agency_id
    name        text        NOT NULL,
    url         text        NOT NULL,                       -- GTFS agency_url (obavezan)
    timezone    text        NOT NULL DEFAULT 'Europe/Zagreb', -- IANA naziv zone
    lang        text        NOT NULL DEFAULT 'hr',          -- BCP 47 oznaka jezika
    phone       text,
    fare_url    text,                                       -- GTFS agency_fare_url
    email       text,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_agencies PRIMARY KEY (id)
);

CREATE UNIQUE INDEX uq_agencies_code ON agencies (code);

-- ---------------------------------------------------------------------
-- stops: stajališta, peroni, stanice (kolodvori) i njihovi dijelovi.
-- GTFS: stops.txt
--   location_type:       0 = stajalište/peron (samo na njima vožnje staju)
--                        1 = stanica (roditelj perona, ulaza i čvorova)
--                        2 = ulaz/izlaz, 3 = čvor (roditelj: stanica)
--                        4 = mjesto ukrcavanja (roditelj: peron)
--   wheelchair_boarding: 0 = nepoznato, 1 = pristupačno, 2 = nepristupačno
--   koordinate su obavezne osim za location_type 3 i 4
-- ---------------------------------------------------------------------
CREATE TABLE stops (
    id                  uuid             NOT NULL DEFAULT gen_random_uuid(),
    code                text             NOT NULL,           -- GTFS stop_id
    public_code         text,                                -- GTFS stop_code (oznaka za putnike)
    name                text             NOT NULL,
    tts_name            text,                                -- GTFS tts_stop_name
    description         text,
    latitude            double precision,
    longitude           double precision,
    zone_id             text,                                -- tarifna zona (fare_rules)
    url                 text,                                -- GTFS stop_url
    location_type       smallint         NOT NULL DEFAULT 0,
    parent_station_id   uuid,
    timezone            text,                                -- GTFS stop_timezone
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
    CONSTRAINT ck_stops_coordinates    CHECK (location_type IN (3, 4) OR (latitude IS NOT NULL AND longitude IS NOT NULL)),
    CONSTRAINT ck_stops_location_type  CHECK (location_type IN (0, 1, 2, 3, 4)),
    CONSTRAINT ck_stops_wheelchair     CHECK (wheelchair_boarding IN (0, 1, 2)),
    CONSTRAINT ck_stops_not_own_parent CHECK (parent_station_id IS NULL OR parent_station_id <> id)
);

CREATE UNIQUE INDEX uq_stops_code              ON stops (code);
CREATE INDEX        ix_stops_parent_station_id ON stops (parent_station_id);

-- ---------------------------------------------------------------------
-- routes: linije. GTFS: routes.txt
--   agency_id: prijevoznik koji vozi liniju (GTFS routes.agency_id);
--              prijevoznik koji ima linije ne može se obrisati
--   route_type: osnovna GTFS vrijednost (3 = autobus) ili proširena
--               (100–1799, npr. 700 = autobus, 200 = međugradski autobus)
--   color, text_color: hex bez znaka #, npr. 'FF0000'
--   continuous_pickup, continuous_drop_off: 0 = bilo gdje na trasi,
--               1 = samo na stanicama, 2 = nazvati, 3 = dogovor s vozačem;
--               NULL = nije zadano (u GTFS-u isto kao 1)
-- ---------------------------------------------------------------------
CREATE TABLE routes (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid(),
    agency_id           uuid        NOT NULL,
    code                text        NOT NULL,          -- GTFS route_id
    short_name          text,                          -- npr. "12"
    long_name           text,                          -- npr. "Osijek - Đakovo"
    description         text,
    route_type          smallint    NOT NULL DEFAULT 3,
    url                 text,                          -- GTFS route_url
    color               char(6),
    text_color          char(6),
    sort_order          integer,                       -- GTFS route_sort_order
    continuous_pickup   smallint,
    continuous_drop_off smallint,
    network_id          text,                          -- GTFS network_id
    is_active           boolean     NOT NULL DEFAULT true,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_routes PRIMARY KEY (id),
    CONSTRAINT fk_routes_agencies_agency_id
        FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE RESTRICT,
    CONSTRAINT ck_routes_name                CHECK (short_name IS NOT NULL OR long_name IS NOT NULL),
    CONSTRAINT ck_routes_color               CHECK (color      IS NULL OR color      ~ '^[0-9A-Fa-f]{6}$'),
    CONSTRAINT ck_routes_text_color          CHECK (text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$'),
    CONSTRAINT ck_routes_continuous_pickup   CHECK (continuous_pickup   IS NULL OR continuous_pickup   IN (0, 1, 2, 3)),
    CONSTRAINT ck_routes_continuous_drop_off CHECK (continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3))
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
    season_id   uuid,                                  -- tip dana sezone (timetable_seasons)
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_service_calendars PRIMARY KEY (id),
    CONSTRAINT ck_service_calendars_dates CHECK (end_date >= start_date)
);

CREATE UNIQUE INDEX uq_service_calendars_code ON service_calendars (code);
CREATE INDEX ix_service_calendars_season_id ON service_calendars (season_id);

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
-- timetable_seasons: sezona voznog reda (npr. "Zimski 2026/27"). Njezini
-- tipovi dana su kalendari (service_calendars.season_id) s razdobljem
-- sezone; iznimke tih kalendara (calendar_dates) izračunavaju se iz
-- season_days i ne uređuju se izravno.
--   holiday_country: ISO kod države za praznike (OpenHolidays API)
--   holiday_mode:    0 = normalno, 1 = vozi kao holiday_calendar_id, 2 = ne vozi
-- ---------------------------------------------------------------------
CREATE TABLE timetable_seasons (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid(),
    name                text        NOT NULL,
    start_date          date        NOT NULL,
    end_date            date        NOT NULL,
    holiday_country     varchar(2),
    holiday_mode        smallint    NOT NULL,
    holiday_calendar_id uuid,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_timetable_seasons PRIMARY KEY (id),
    CONSTRAINT fk_timetable_seasons_service_calendars_holiday_calendar_id
        FOREIGN KEY (holiday_calendar_id) REFERENCES service_calendars (id) ON DELETE SET NULL,
    CONSTRAINT ck_timetable_seasons_dates CHECK (end_date >= start_date),
    CONSTRAINT ck_timetable_seasons_holiday_mode CHECK (holiday_mode IN (0, 1, 2))
);

ALTER TABLE service_calendars
    ADD CONSTRAINT fk_service_calendars_timetable_seasons_season_id
        FOREIGN KEY (season_id) REFERENCES timetable_seasons (id) ON DELETE CASCADE;

-- ---------------------------------------------------------------------
-- season_days: dani sezone koji odstupaju od dana u tjednu
--   mode:   0 = normalno, 1 = vozi kao calendar_id, 2 = ne vozi
--   source: 0 = ručno (ostaje kad se promijeni pravilo za praznike),
--           1 = praznik (note je naziv praznika)
-- ---------------------------------------------------------------------
CREATE TABLE season_days (
    season_id   uuid     NOT NULL,
    date        date     NOT NULL,
    mode        smallint NOT NULL,
    calendar_id uuid,
    source      smallint NOT NULL,
    note        text,

    CONSTRAINT pk_season_days PRIMARY KEY (season_id, date),
    CONSTRAINT fk_season_days_timetable_seasons_season_id
        FOREIGN KEY (season_id) REFERENCES timetable_seasons (id) ON DELETE CASCADE,
    CONSTRAINT fk_season_days_service_calendars_calendar_id
        FOREIGN KEY (calendar_id) REFERENCES service_calendars (id) ON DELETE CASCADE,
    CONSTRAINT ck_season_days_mode CHECK (mode IN (0, 1, 2)),
    CONSTRAINT ck_season_days_source CHECK (source IN (0, 1))
);

CREATE INDEX ix_season_days_calendar_id ON season_days (calendar_id);

-- ---------------------------------------------------------------------
-- route_variants, route_variant_stops: varijante linije (redoslijed
-- stanica smjera s vremenima vožnje). Nisu dio GTFS-a; predložak su za
-- nove vožnje (trips.variant_id), a izvoz koristi vremena vožnji.
--   source:      0 = ručno, 1 = prepoznata iz vožnji (GTFS uvoz)
--   travel_time: sekunde od polaska s prethodne stanice (0 za prvu)
--   dwell_time:  sekunde stajanja na stanici
-- ---------------------------------------------------------------------
CREATE TABLE route_variants (
    id           uuid        NOT NULL DEFAULT gen_random_uuid(),
    route_id     uuid        NOT NULL,
    name         text        NOT NULL,
    direction_id smallint,
    headsign     text,
    shape_id     uuid,
    sort_order   integer     NOT NULL,
    source       smallint    NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_route_variants PRIMARY KEY (id),
    CONSTRAINT fk_route_variants_routes_route_id
        FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE,
    CONSTRAINT fk_route_variants_shapes_shape_id
        FOREIGN KEY (shape_id) REFERENCES shapes (id) ON DELETE SET NULL,
    CONSTRAINT ck_route_variants_direction CHECK (direction_id IS NULL OR direction_id IN (0, 1)),
    CONSTRAINT ck_route_variants_source CHECK (source IN (0, 1))
);

CREATE INDEX ix_route_variants_route_id ON route_variants (route_id);
CREATE INDEX ix_route_variants_shape_id ON route_variants (shape_id);

CREATE TABLE route_variant_stops (
    variant_id  uuid    NOT NULL,
    sequence    integer NOT NULL,
    stop_id     uuid    NOT NULL,
    travel_time integer NOT NULL,
    dwell_time  integer NOT NULL,

    CONSTRAINT pk_route_variant_stops PRIMARY KEY (variant_id, sequence),
    CONSTRAINT fk_route_variant_stops_route_variants_variant_id
        FOREIGN KEY (variant_id) REFERENCES route_variants (id) ON DELETE CASCADE,
    CONSTRAINT fk_route_variant_stops_stops_stop_id
        FOREIGN KEY (stop_id) REFERENCES stops (id),
    CONSTRAINT ck_route_variant_stops_times CHECK (travel_time >= 0 AND dwell_time >= 0)
);

CREATE INDEX ix_route_variant_stops_stop_id ON route_variant_stops (stop_id);

ALTER TABLE trips
    ADD COLUMN variant_id uuid,
    ADD CONSTRAINT fk_trips_route_variants_variant_id
        FOREIGN KEY (variant_id) REFERENCES route_variants (id) ON DELETE SET NULL;

CREATE INDEX ix_trips_variant_id ON trips (variant_id);

-- ---------------------------------------------------------------------
-- shapes, shape_points: trasa vožnje na karti. GTFS: shapes.txt
--   dist_traveled: prijeđena udaljenost od početka trase, u jedinicama
--                  po izboru (iste kao stop_times.shape_dist_traveled)
-- ---------------------------------------------------------------------
CREATE TABLE shapes (
    id          uuid        NOT NULL DEFAULT gen_random_uuid(),
    code        text        NOT NULL,                  -- GTFS shape_id
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_shapes PRIMARY KEY (id)
);

CREATE UNIQUE INDEX uq_shapes_code ON shapes (code);

CREATE TABLE shape_points (
    shape_id      uuid             NOT NULL,
    sequence      integer          NOT NULL,           -- 1, 2, 3 ...
    latitude      double precision NOT NULL,
    longitude     double precision NOT NULL,
    dist_traveled double precision,

    CONSTRAINT pk_shape_points PRIMARY KEY (shape_id, sequence),
    CONSTRAINT fk_shape_points_shapes_shape_id
        FOREIGN KEY (shape_id) REFERENCES shapes (id) ON DELETE CASCADE,
    CONSTRAINT ck_shape_points_sequence  CHECK (sequence >= 0),
    CONSTRAINT ck_shape_points_latitude  CHECK (latitude  BETWEEN -90  AND 90),
    CONSTRAINT ck_shape_points_longitude CHECK (longitude BETWEEN -180 AND 180),
    CONSTRAINT ck_shape_points_dist      CHECK (dist_traveled IS NULL OR dist_traveled >= 0)
);

-- ---------------------------------------------------------------------
-- trips: pojedinačna vožnja linije, npr. Osijek -> Đakovo u 06:15.
-- GTFS: trips.txt
--
-- service_calendar_id koristi NO ACTION: kalendar koji koriste vožnje ne
-- može se obrisati. Brisanje trase ostavlja vožnje bez nje (SET NULL).
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
    shape_id              uuid,                         -- GTFS shape_id
    wheelchair_accessible smallint    NOT NULL DEFAULT 0,
    bikes_allowed         smallint    NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_trips PRIMARY KEY (id),
    CONSTRAINT fk_trips_routes_route_id
        FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE,
    CONSTRAINT fk_trips_service_calendars_service_calendar_id
        FOREIGN KEY (service_calendar_id) REFERENCES service_calendars (id) ON DELETE NO ACTION,
    CONSTRAINT fk_trips_shapes_shape_id
        FOREIGN KEY (shape_id) REFERENCES shapes (id) ON DELETE SET NULL,
    CONSTRAINT ck_trips_direction  CHECK (direction_id IS NULL OR direction_id IN (0, 1)),
    CONSTRAINT ck_trips_wheelchair CHECK (wheelchair_accessible IN (0, 1, 2)),
    CONSTRAINT ck_trips_bikes      CHECK (bikes_allowed IN (0, 1, 2))
);

CREATE UNIQUE INDEX uq_trips_code                 ON trips (code);
CREATE INDEX        ix_trips_route_id             ON trips (route_id);
CREATE INDEX        ix_trips_service_calendar_id  ON trips (service_calendar_id);
CREATE INDEX        ix_trips_shape_id             ON trips (shape_id);

-- ---------------------------------------------------------------------
-- stop_times: dolazak i polazak vožnje na svakoj stanici. GTFS: stop_times.txt
--
-- Vremena su u sekundama od ponoći dana vožnje. GTFS dopušta vrijednosti
-- iznad 24 h za vožnje koje prelaze ponoć (25:30:00 = 91800); gornja
-- granica je 48 h. Vremena su obavezna na svakoj stanici; okvirna vremena
-- označava timepoint = false.
--   pickup_type, drop_off_type: 0 = redovno, 1 = nema, 2 = nazvati
--                               prijevoznika, 3 = dogovor s vozačem
--   continuous_pickup, continuous_drop_off: kao kod routes; NULL znači
--                               vrijednost linije
--   timepoint: true = točno vrijeme, false = okvirno
--
-- stop_id koristi NO ACTION: stanica koja se koristi u voznom redu ne može
-- se obrisati.
-- ---------------------------------------------------------------------
CREATE TABLE stop_times (
    trip_id             uuid             NOT NULL,
    stop_sequence       integer          NOT NULL,     -- redni broj na vožnji, raste
    stop_id             uuid             NOT NULL,
    arrival_time        integer          NOT NULL,
    departure_time      integer          NOT NULL,
    stop_headsign       text,
    pickup_type         smallint         NOT NULL DEFAULT 0,
    drop_off_type       smallint         NOT NULL DEFAULT 0,
    continuous_pickup   smallint,
    continuous_drop_off smallint,
    shape_dist_traveled double precision,
    timepoint           boolean          NOT NULL DEFAULT true,

    CONSTRAINT pk_stop_times PRIMARY KEY (trip_id, stop_sequence),
    CONSTRAINT fk_stop_times_trips_trip_id
        FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE,
    CONSTRAINT fk_stop_times_stops_stop_id
        FOREIGN KEY (stop_id) REFERENCES stops (id) ON DELETE NO ACTION,
    CONSTRAINT ck_stop_times_sequence            CHECK (stop_sequence >= 0),
    CONSTRAINT ck_stop_times_arrival             CHECK (arrival_time   BETWEEN 0 AND 172800),
    CONSTRAINT ck_stop_times_departure           CHECK (departure_time BETWEEN 0 AND 172800),
    CONSTRAINT ck_stop_times_order               CHECK (departure_time >= arrival_time),
    CONSTRAINT ck_stop_times_pickup              CHECK (pickup_type   IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_dropoff             CHECK (drop_off_type IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_continuous_pickup   CHECK (continuous_pickup   IS NULL OR continuous_pickup   IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_continuous_drop_off CHECK (continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)),
    CONSTRAINT ck_stop_times_shape_dist          CHECK (shape_dist_traveled IS NULL OR shape_dist_traveled >= 0)
);

-- Polasci sa stanice poredani po vremenu (vozni red stanice, prikaz polazaka).
-- Pokriva i strani ključ stop_id.
CREATE INDEX ix_stop_times_stop_departure ON stop_times (stop_id, departure_time);

-- ---------------------------------------------------------------------
-- fares, fare_rules: cijene karata i pravila primjene (GTFS Fares v1:
-- fare_attributes.txt, fare_rules.txt)
--   payment_method: 0 = u vozilu, 1 = prije ukrcavanja
--   transfers: 0, 1 ili 2 presjedanja; NULL = neograničeno
--   transfer_duration: koliko sekundi karta vrijedi za presjedanja
--   origin_id, destination_id, contains_id: vrijednosti stops.zone_id
--
-- Cijena bez pravila vrijedi za sve vožnje. Prijevoznik s cijenama ne može
-- se obrisati; brisanje linije briše pravila koja se na nju odnose.
-- ---------------------------------------------------------------------
CREATE TABLE fares (
    id                uuid          NOT NULL DEFAULT gen_random_uuid(),
    code              text          NOT NULL,          -- GTFS fare_id
    price             numeric(12,4) NOT NULL,
    currency_type     char(3)       NOT NULL,          -- ISO 4217, npr. 'EUR'
    payment_method    smallint      NOT NULL,
    transfers         smallint,
    agency_id         uuid,
    transfer_duration integer,
    created_at        timestamptz   NOT NULL DEFAULT now(),
    updated_at        timestamptz   NOT NULL DEFAULT now(),

    CONSTRAINT pk_fares PRIMARY KEY (id),
    CONSTRAINT fk_fares_agencies_agency_id
        FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE RESTRICT,
    CONSTRAINT ck_fares_price             CHECK (price >= 0),
    CONSTRAINT ck_fares_currency          CHECK (currency_type ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_fares_payment_method    CHECK (payment_method IN (0, 1)),
    CONSTRAINT ck_fares_transfers         CHECK (transfers IS NULL OR transfers IN (0, 1, 2)),
    CONSTRAINT ck_fares_transfer_duration CHECK (transfer_duration IS NULL OR transfer_duration >= 0)
);

CREATE UNIQUE INDEX uq_fares_code      ON fares (code);
CREATE INDEX        ix_fares_agency_id ON fares (agency_id);

CREATE TABLE fare_rules (
    id             uuid    NOT NULL DEFAULT gen_random_uuid(),
    fare_id        uuid    NOT NULL,
    sequence       integer NOT NULL,                   -- redoslijed unutar cijene
    route_id       uuid,
    origin_id      text,
    destination_id text,
    contains_id    text,

    CONSTRAINT pk_fare_rules PRIMARY KEY (id),
    CONSTRAINT fk_fare_rules_fares_fare_id
        FOREIGN KEY (fare_id) REFERENCES fares (id) ON DELETE CASCADE,
    CONSTRAINT fk_fare_rules_routes_route_id
        FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX uq_fare_rules_fare_sequence ON fare_rules (fare_id, sequence);
CREATE INDEX        ix_fare_rules_route_id      ON fare_rules (route_id);

-- ---------------------------------------------------------------------
-- feed_info: izdavač feeda (GTFS feed_info.txt). Najviše jedan redak.
-- Razdoblje važenja i verzija izračunavaju se pri izvozu. Bez retka
-- izvoz kao izdavača koristi prvog prijevoznika.
-- ---------------------------------------------------------------------
CREATE TABLE feed_info (
    id             smallint    NOT NULL,
    publisher_name text        NOT NULL,
    publisher_url  text        NOT NULL,
    lang           text        NOT NULL,               -- npr. 'hr' ili 'mul'
    default_lang   text,
    contact_email  text,
    contact_url    text,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_feed_info PRIMARY KEY (id),
    CONSTRAINT ck_feed_info_single_row CHECK (id = 1)
);

-- ---------------------------------------------------------------------
-- settings: postavke instance za javne stranice i PDF. Najviše jedan
-- redak; dok ga nema, vrijede zadane vrijednosti. Logo (PNG, JPEG, WebP
-- ili SVG, najviše 512 KB) čita se zasebno, ne uz ostale postavke.
-- ---------------------------------------------------------------------
CREATE TABLE settings (
    id                  smallint    NOT NULL,
    display_name        text,                          -- bez njega naziv prvog prijevoznika
    primary_color       text,                          -- hex bez #, npr. '1565C0'
    primary_text_color  text,
    public_url          text,                          -- npr. 'https://voznired.gpp.hr' (QR kodovi)
    public_gtfs_enabled boolean     NOT NULL DEFAULT false,
    logo                bytea,
    logo_content_type   text,
    logo_updated_at     timestamptz,
    setup_completed_at  timestamptz,                    -- kad je čarobnjak prvog pokretanja završen
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pk_settings PRIMARY KEY (id),
    CONSTRAINT ck_settings_single_row CHECK (id = 1),
    CONSTRAINT ck_settings_primary_color CHECK (primary_color ~ '^[0-9A-F]{6}$'),
    CONSTRAINT ck_settings_primary_text_color CHECK (primary_text_color ~ '^[0-9A-F]{6}$'),
    CONSTRAINT ck_settings_logo CHECK ((logo IS NULL) = (logo_content_type IS NULL)
                                   AND (logo IS NULL) = (logo_updated_at IS NULL))
);

-- ---------------------------------------------------------------------
-- admin_users: administratori (ASP.NET Core Identity, bez uloga).
-- user_name = email. Pomoćne Identity tablice admin_user_claims,
-- admin_user_logins i admin_user_tokens zasad se ne koriste.
-- ---------------------------------------------------------------------
CREATE TABLE admin_users (
    id                     uuid         NOT NULL,
    user_name              varchar(256),
    normalized_user_name   varchar(256),
    email                  varchar(256),
    normalized_email       varchar(256),
    email_confirmed        boolean      NOT NULL,
    password_hash          text,
    security_stamp         text,                       -- mijenja se s lozinkom; poništava ostale prijave
    concurrency_stamp      text,
    phone_number           text,
    phone_number_confirmed boolean      NOT NULL,
    two_factor_enabled     boolean      NOT NULL,
    lockout_end            timestamptz,                -- zaključan do (nakon 5 neuspjelih prijava)
    lockout_enabled        boolean      NOT NULL,
    access_failed_count    integer      NOT NULL,
    created_at             timestamptz  NOT NULL DEFAULT now(),
    last_login_at          timestamptz,

    CONSTRAINT pk_admin_users PRIMARY KEY (id)
);

CREATE UNIQUE INDEX uq_admin_users_normalized_user_name ON admin_users (normalized_user_name);
CREATE INDEX        ix_admin_users_normalized_email     ON admin_users (normalized_email);

-- ---------------------------------------------------------------------
-- data_protection_keys: ključevi za šifriranje kolačića prijave, da
-- prijava preživi restart i radi na više instanci aplikacije.
-- ---------------------------------------------------------------------
CREATE TABLE data_protection_keys (
    id            integer GENERATED BY DEFAULT AS IDENTITY,
    friendly_name text,
    xml           text,

    CONSTRAINT pk_data_protection_keys PRIMARY KEY (id)
);
