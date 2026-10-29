CREATE OR REPLACE FUNCTION project.fn_get_projects(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', (
        SELECT COALESCE(jsonb_agg(jsonb_build_object(
            'id', id, 'name', name, 'description', description,
            'created_at', created_at, 'updated_at', updated_at
        ) ORDER BY updated_at DESC), '[]'::jsonb)
        FROM project.projects
        WHERE user_id = p_actor_id
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_get_projects failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
