CREATE SCHEMA IF NOT EXISTS task;

CREATE TABLE IF NOT EXISTS task.tasks (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id  uuid NOT NULL,
    -- Denormalized from Project.Api at creation time (verified via a synchronous ownership
    -- check through Clients/ProjectServiceClient before this row is written). No cross-service
    -- join is possible since this is a separate database, so every later read/write checks
    -- ownership against THIS column directly instead of re-calling Project.Api each time.
    user_id     uuid NOT NULL,
    title       text NOT NULL CHECK (length(title) BETWEEN 1 AND 200),
    description text CHECK (description IS NULL OR length(description) <= 2000),
    status      text NOT NULL DEFAULT 'Todo' CHECK (status IN ('Todo','InProgress','Done')),
    priority    text NOT NULL DEFAULT 'Medium' CHECK (priority IN ('Low','Medium','High')),
    due_date    date,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_tasks_project_id ON task.tasks(project_id);
CREATE INDEX IF NOT EXISTS ix_tasks_user_id ON task.tasks(user_id);
