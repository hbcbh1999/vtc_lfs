-- Table: public.movement

-- DROP TABLE public.movement;

CREATE TABLE public.movement
(
    id bigint NOT NULL DEFAULT nextval('movement_id_seq'::regclass),
    job_id integer NOT NULL DEFAULT nextval('movement_job_id_seq'::regclass),
    approach text COLLATE pg_catalog."default",
    exit text COLLATE pg_catalog."default",
    movementtype text COLLATE pg_catalog."default",
    objecttype text COLLATE pg_catalog."default",
    synthetic boolean,
    CONSTRAINT movement_pkey PRIMARY KEY (id),
    CONSTRAINT job_fkey FOREIGN KEY (job_id)
        REFERENCES public.job (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE public.movement
    OWNER to postgres;