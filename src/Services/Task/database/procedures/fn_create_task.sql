-- C# has already verified (via a synchronous call to Project.Api) that p_actor_id owns
-- project_id before calling this. The function trusts p_actor_id as the task's owner — it
-- cannot re-verify project ownership itself since projects live in a different database.
CREATE OR REPLACE FUNCTION task.fn_create_task(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = task, pg_temp
AS $$
DECLARE
    v_project_id  uuid;
    v_title       text;
    v_description text;
    v_priority    text;
    v_due_date    date;
    v_id          uuid;
    v_created_at  timestamptz;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_project_id := (p_payload->>'project_id')::uuid;
        v_due_date   := NULLIF(p_payload->>'due_date', '')::date;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed input', 'data', NULL);
    END;

    v_title       := NULLIF(btrim(p_payload->>'title'), '');
    v_description := NULLIF(btrim(p_payload->>'description'), '');
    v_priority    := COALESCE(NULLIF(p_payload->>'priority', ''), 'Medium');

    IF v_project_id IS NULL OR v_title IS NULL OR length(v_title) > 200 THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'project_id and title are required', 'data', NULL);
    END IF;

    IF v_priority NOT IN ('Low', 'Medium', 'High') THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Invalid priority', 'data', NULL);
    END IF;

    INSERT INTO task.tasks (project_id, user_id, title, description, priority, due_date)
    VALUES (v_project_id, p_actor_id, v_title, v_description, v_priority, v_due_date)
    RETURNING id, created_at INTO v_id, v_created_at;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Created', 'data', jsonb_build_object(
        'id', v_id, 'project_id', v_project_id, 'title', v_title, 'description', v_description,
        'status', 'Todo', 'priority', v_priority, 'due_date', v_due_date,
        'created_at', v_created_at, 'updated_at', v_created_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_create_task failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
