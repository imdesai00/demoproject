CREATE SCHEMA IF NOT EXISTS project;

CREATE TABLE IF NOT EXISTS project.projects (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     uuid NOT NULL,
    name        text NOT NULL CHECK (length(name) BETWEEN 1 AND 200),
    description text CHECK (description IS NULL OR length(description) <= 2000),
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_projects_user_id ON project.projects(user_id);

-- Transactional outbox: fn_delete_project (and any future cross-service-relevant change)
-- writes its event here in the SAME transaction as the data change, so the DB write and the
-- "something happened" record can never disagree. A background publisher (OutboxPublisherHostedService)
-- polls this table and ships rows to RabbitMQ, then marks them published.
CREATE TABLE IF NOT EXISTS project.outbox_events (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type   text NOT NULL,
    routing_key  text NOT NULL,
    payload      jsonb NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz
);

CREATE INDEX IF NOT EXISTS ix_outbox_events_unpublished ON project.outbox_events(created_at) WHERE published_at IS NULL;
