-- Table: public.stateestimate

-- DROP TABLE public.stateestimate;

CREATE TABLE public.stateestimate
(
    id bigint NOT NULL DEFAULT nextval('stateestimate_id_seq'::regclass),
    movement_id bigint NOT NULL DEFAULT nextval('stateestimate_movement_id_seq'::regclass),
    x real,
    y real,
    vx real,
    vy real,
    red real,
    blue real,
    green real,
    size real,
    vsize real,
    pathlength real,
    CONSTRAINT stateestimate_pkey PRIMARY KEY (id),
    CONSTRAINT movement_fkey FOREIGN KEY (movement_id)
        REFERENCES public.movement (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE public.stateestimate
    OWNER to postgres;