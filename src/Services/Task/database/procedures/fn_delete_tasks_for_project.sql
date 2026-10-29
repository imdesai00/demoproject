-- Internal-only: invoked by the RabbitMQ consumer handling "project.deleted" (actor role
-- 'system'), never reachable from an HTTP request. No user_id filter — the project is gone,
-- every task under it is deleted regardless of which single user "owns" the rows (there's only
-- ever one, but we don't bother re-checking since the triggering project is already gone).
CREATE OR REPLACE FUNCTION task.fn_delete_tasks_for_project(
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
    v_count      int;
BEGIN
    IF p_actor_role <> 'system' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Internal operation only', 'data', NULL);
    END IF;

    BEGIN
        v_project_id := (p_payload->>'project_id')::uuid;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed project_id', 'data', NULL);
    END;

    DELETE FROM task.tasks WHERE project_id = v_project_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Deleted', 'data', jsonb_build_object('deleted_count', v_count));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_delete_tasks_for_project failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
