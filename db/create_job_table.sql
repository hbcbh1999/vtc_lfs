-- Table: public.job

-- DROP TABLE public.job;

CREATE TABLE public.job
(
    created_at timestamp with time zone,
    groundtruthpath path,
    regionconfiguration text COLLATE pg_catalog."default",
    videopath path,
    id integer NOT NULL DEFAULT nextval('job_id_seq'::regclass),
    CONSTRAINT job_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.job
    OWNER to postgres;