CREATE SCHEMA IF NOT EXISTS identity;

CREATE TABLE IF NOT EXISTS identity.users (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email         text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    display_name  text NOT NULL CHECK (length(display_name) BETWEEN 1 AND 100),
    created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.refresh_tokens (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    token_hash             text NOT NULL UNIQUE,
    expires_at             timestamptz NOT NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    revoked_at             timestamptz,
    replaced_by_token_hash text
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON identity.refresh_tokens(user_id);
