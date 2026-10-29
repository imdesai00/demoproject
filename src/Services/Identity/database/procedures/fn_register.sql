-- identity.fn_register(p_actor_id, p_actor_role, {email, password_hash, display_name})
-- Anonymous endpoint: C# passes ActorContext.Anonymous (Guid.Empty / 'anonymous'). Password
-- hashing happens in C# (BCrypt) before this is called — only the hash ever reaches the DB.
CREATE OR REPLACE FUNCTION identity.fn_register(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, pg_temp
AS $$
DECLARE
    v_email         text;
    v_password_hash text;
    v_display_name  text;
    v_id            uuid;
    v_created_at    timestamptz;
BEGIN
    v_email         := NULLIF(lower(btrim(p_payload->>'email')), '');
    v_password_hash := NULLIF(p_payload->>'password_hash', '');
    v_display_name  := NULLIF(btrim(p_payload->>'display_name'), '');

    IF v_email IS NULL OR v_password_hash IS NULL OR v_display_name IS NULL THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION',
                                  'message', 'email, password_hash and display_name are required', 'data', NULL);
    END IF;

    INSERT INTO identity.users (email, password_hash, display_name)
    VALUES (v_email, v_password_hash, v_display_name)
    RETURNING id, created_at INTO v_id, v_created_at;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Created', 'data', jsonb_build_object(
        'id', v_id, 'email', v_email, 'display_name', v_display_name, 'created_at', v_created_at
    ));

EXCEPTION
    WHEN unique_violation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'CONFLICT',
                                  'message', 'An account with this email already exists', 'data', NULL);
    WHEN OTHERS THEN
        RAISE WARNING 'fn_register failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
