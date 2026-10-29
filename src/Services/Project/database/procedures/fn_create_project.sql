CREATE OR REPLACE FUNCTION project.fn_create_project(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
DECLARE
    v_name        text;
    v_description text;
    v_id          uuid;
    v_created_at  timestamptz;
BEGIN
    IF p_actor_id IS NULL OR p_actor_id = '00000000-0000-0000-0000-000000000000' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'Not authenticated', 'data', NULL);
    END IF;

    v_name        := NULLIF(btrim(p_payload->>'name'), '');
    v_description := NULLIF(btrim(p_payload->>'description'), '');

    IF v_name IS NULL OR length(v_name) > 200 THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'name is required (max 200 chars)', 'data', NULL);
    END IF;

    INSERT INTO project.projects (user_id, name, description)
    VALUES (p_actor_id, v_name, v_description)
    RETURNING id, created_at INTO v_id, v_created_at;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Created', 'data', jsonb_build_object(
        'id', v_id, 'name', v_name, 'description', v_description,
        'created_at', v_created_at, 'updated_at', v_created_at
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_create_project failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
