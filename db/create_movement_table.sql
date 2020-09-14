-- Table: public.movement

-- DROP TABLE public.movement;

CREATE TABLE public.movement
(
    id bigint NOT NULL DEFAULT nextval('movement_id_seq'::regclass),
    approach text COLLATE pg_catalog."default",
    exit text COLLATE pg_catalog."default",
    turntype text COLLATE pg_catalog."default",
    trafficobjecttype text COLLATE pg_catalog."default",
    "timestamp" timestamp with time zone,
    ignored boolean,
    synthetic boolean,
    jobid integer NOT NULL DEFAULT nextval('"movement_JobId_seq"'::regclass),
    CONSTRAINT movement_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.movement
    OWNER to postgres;