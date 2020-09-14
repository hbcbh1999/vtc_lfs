-- Table: public.job

-- DROP TABLE public.job;

CREATE TABLE public.job
(
    id integer NOT NULL DEFAULT nextval('job_id_seq'::regclass),
    videopath text COLLATE pg_catalog."default",
    groundtruthpath text COLLATE pg_catalog."default",
    regionconfigurationname text COLLATE pg_catalog."default",
    "timestamp" time with time zone,
    CONSTRAINT job_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.job
    OWNER to postgres;