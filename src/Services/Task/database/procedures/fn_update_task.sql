CREATE OR REPLACE FUNCTION task.fn_update_task(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = task, pg_temp
AS $$
DECLARE
    v_task_id     uuid;
    v_title       text;
    v_description text;
    v_priority    text;
    v_due_date    date;
    v_row         task.tasks%ROWTYPE;
    v_updated_at  timestamptz;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_task_id  := (p_payload->>'task_id')::uuid;
        v_due_date := NULLIF(p_payload->>'due_date', '')::date;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed input', 'data', NULL);
    END;

    v_title       := NULLIF(btrim(p_payload->>'title'), '');
    v_description := NULLIF(btrim(p_payload->>'description'), '');
    v_priority    := COALESCE(NULLIF(p_payload->>'priority', ''), 'Medium');

    IF v_title IS NULL OR length(v_title) > 200 THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'title is required', 'data', NULL);
    END IF;

    IF v_priority NOT IN ('Low', 'Medium', 'High') THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Invalid priority', 'data', NULL);
    END IF;

    SELECT * INTO v_row FROM task.tasks WHERE id = v_task_id FOR UPDATE;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'NOT_FOUND', 'message', 'Task not found', 'data', NULL);
    END IF;

    IF v_row.user_id <> p_actor_id THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Not your task', 'data', NULL);
    END IF;

    UPDATE task.tasks
    SET title = v_title, description = v_description, priority = v_priority, due_date = v_due_date, updated_at = now()
    WHERE id = v_task_id
    RETURNING updated_at INTO v_updated_at;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Updated', 'data', jsonb_build_object(
        'id', v_task_id, 'project_id', v_row.project_id, 'title', v_title, 'description', v_description,
        'status', v_row.status, 'priority', v_priority, 'due_date', v_due_date,
        'created_at', v_row.created_at, 'updated_at', v_updated_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_update_task failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
