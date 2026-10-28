-- identity.fn_create_refresh_token(p_actor_id, p_actor_role, {user_id, token_hash, expires_at})
-- Called right after a successful register/login, so actor is still Anonymous — authorization
-- here is "trust the caller, this is an internal detail of issuing tokens", not user-scoped.
CREATE OR REPLACE FUNCTION identity.fn_create_refresh_token(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, pg_temp
AS $$
DECLARE
    v_user_id    uuid;
    v_token_hash text;
    v_expires_at timestamptz;
BEGIN
    BEGIN
        v_user_id    := (p_payload->>'user_id')::uuid;
        v_expires_at := (p_payload->>'expires_at')::timestamptz;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed input', 'data', NULL);
    END;
    v_token_hash := NULLIF(p_payload->>'token_hash', '');

    IF v_user_id IS NULL OR v_token_hash IS NULL OR v_expires_at IS NULL THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Missing fields', 'data', NULL);
    END IF;

    INSERT INTO identity.refresh_tokens (user_id, token_hash, expires_at)
    VALUES (v_user_id, v_token_hash, v_expires_at);

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Created', 'data', NULL);

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_create_refresh_token failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
