CREATE OR REPLACE FUNCTION project.fn_update_project(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
DECLARE
    v_project_id  uuid;
    v_name        text;
    v_description text;
    v_row         project.projects%ROWTYPE;
    v_updated_at  timestamptz;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    BEGIN
        v_project_id := (p_payload->>'project_id')::uuid;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed project_id', 'data', NULL);
    END;

    v_name        := NULLIF(btrim(p_payload->>'name'), '');
    v_description := NULLIF(btrim(p_payload->>'description'), '');

    IF v_name IS NULL OR length(v_name) > 200 THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'name is required (max 200 chars)', 'data', NULL);
    END IF;

    SELECT * INTO v_row FROM project.projects WHERE id = v_project_id FOR UPDATE;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'NOT_FOUND', 'message', 'Project not found', 'data', NULL);
    END IF;

    IF v_row.user_id <> p_actor_id THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Not your project', 'data', NULL);
    END IF;

    UPDATE project.projects
    SET name = v_name, description = v_description, updated_at = now()
    WHERE id = v_project_id
    RETURNING updated_at INTO v_updated_at;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Updated', 'data', jsonb_build_object(
        'id', v_project_id, 'name', v_name, 'description', v_description,
        'created_at', v_row.created_at, 'updated_at', v_updated_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_update_project failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
