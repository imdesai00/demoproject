CREATE OR REPLACE FUNCTION task.fn_get_tasks_for_project(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = task, pg_temp
AS $$
DECLARE
    v_project_id uuid;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_project_id := (p_payload->>'project_id')::uuid;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed project_id', 'data', NULL);
    END;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', (
        SELECT COALESCE(jsonb_agg(jsonb_build_object(
            'id', id, 'project_id', project_id, 'title', title, 'description', description,
            'status', status, 'priority', priority, 'due_date', due_date,
            'created_at', created_at, 'updated_at', updated_at
        ) ORDER BY created_at), '[]'::jsonb)
        FROM task.tasks
        WHERE project_id = v_project_id AND user_id = p_actor_id
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_get_tasks_for_project failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
