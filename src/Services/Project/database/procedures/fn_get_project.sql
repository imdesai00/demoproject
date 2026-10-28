CREATE OR REPLACE FUNCTION project.fn_get_project(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
DECLARE
    v_project_id uuid;
    v_row        project.projects%ROWTYPE;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_project_id := (p_payload->>'project_id')::uuid;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed project_id', 'data', NULL);
    END;

    SELECT * INTO v_row FROM project.projects WHERE id = v_project_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'NOT_FOUND', 'message', 'Project not found', 'data', NULL);
    END IF;

    IF v_row.user_id <> p_actor_id THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Not your project', 'data', NULL);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', jsonb_build_object(
        'id', v_row.id, 'name', v_row.name, 'description', v_row.description,
        'created_at', v_row.created_at, 'updated_at', v_row.updated_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_get_project failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
