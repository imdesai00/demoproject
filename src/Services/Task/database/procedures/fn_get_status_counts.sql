-- Bulk status counts for the dashboard/project-list view. Called by Project.Api with the
-- SAME end user's forwarded bearer token (not an internal/system call), so ownership filtering
-- (user_id = p_actor_id) still applies — Project.Api can't use this to see another user's data.
CREATE OR REPLACE FUNCTION task.fn_get_status_counts(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = task, pg_temp
AS $$
DECLARE
    v_project_ids uuid[];
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        SELECT array_agg((elem)::uuid) INTO v_project_ids
        FROM jsonb_array_elements_text(p_payload->'project_ids') AS elem;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed project_ids', 'data', NULL);
    END;

    IF v_project_ids IS NULL OR array_length(v_project_ids, 1) IS NULL THEN
        RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', '[]'::jsonb);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', (
        SELECT COALESCE(jsonb_agg(jsonb_build_object(
            'project_id', project_id, 'todo', todo, 'in_progress', in_progress, 'done', done
        )), '[]'::jsonb)
        FROM (
            SELECT
                project_id,
                count(*) FILTER (WHERE status = 'Todo') AS todo,
                count(*) FILTER (WHERE status = 'InProgress') AS in_progress,
                count(*) FILTER (WHERE status = 'Done') AS done
            FROM task.tasks
            WHERE project_id = ANY(v_project_ids) AND user_id = p_actor_id
            GROUP BY project_id
        ) counts
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_get_status_counts failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
