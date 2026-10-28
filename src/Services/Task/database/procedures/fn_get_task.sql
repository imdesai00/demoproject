CREATE OR REPLACE FUNCTION task.fn_get_task(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = task, pg_temp
AS $$
DECLARE
    v_task_id uuid;
    v_row     task.tasks%ROWTYPE;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_task_id := (p_payload->>'task_id')::uuid;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed task_id', 'data', NULL);
    END;

    SELECT * INTO v_row FROM task.tasks WHERE id = v_task_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'NOT_FOUND', 'message', 'Task not found', 'data', NULL);
    END IF;

    IF v_row.user_id <> p_actor_id THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Not your task', 'data', NULL);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', jsonb_build_object(
        'id', v_row.id, 'project_id', v_row.project_id, 'title', v_row.title, 'description', v_row.description,
        'status', v_row.status, 'priority', v_row.priority, 'due_date', v_row.due_date,
        'created_at', v_row.created_at, 'updated_at', v_row.updated_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_get_task failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
