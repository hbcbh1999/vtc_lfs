-- Table: public.stateestimate

-- DROP TABLE public.stateestimate;

CREATE TABLE public.stateestimate
(
    id bigint NOT NULL DEFAULT nextval('stateestimate_id_seq'::regclass),
    x double precision,
    y double precision,
    covx double precision,
    covy double precision,
    vx double precision,
    vy double precision,
    covvx double precision[],
    covvy double precision,
    red double precision,
    green double precision,
    blue double precision,
    covred double precision,
    covgreen double precision,
    covblue double precision,
    size double precision,
    covsize double precision,
    vsize double precision,
    pathlength double precision,
    movementid bigint NOT NULL DEFAULT nextval('"stateestimate_MovementId_seq"'::regclass),
    totalmisseddetections integer,
    successivemisseddetections integer,
    CONSTRAINT stateestimate_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.stateestimate
    OWNER to postgres;