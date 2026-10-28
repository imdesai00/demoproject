-- identity.fn_rotate_refresh_token(p_actor_id, p_actor_role, {old_token_hash, new_token_hash, new_expires_at})
-- Atomically revokes the presented refresh token and issues a new one, in one transaction
-- (the function call itself) so there's no window where a token is revoked but no replacement
-- exists. Returns the owning user so C# can mint a fresh access token without a second lookup.
CREATE OR REPLACE FUNCTION identity.fn_rotate_refresh_token(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, pg_temp
AS $$
DECLARE
    v_old_hash      text;
    v_new_hash      text;
    v_new_expires   timestamptz;
    v_token_row     identity.refresh_tokens%ROWTYPE;
    v_user          identity.users%ROWTYPE;
BEGIN
    v_old_hash := NULLIF(p_payload->>'old_token_hash', '');
    v_new_hash := NULLIF(p_payload->>'new_token_hash', '');
    BEGIN
        v_new_expires := (p_payload->>'new_expires_at')::timestamptz;
    EXCEPTION WHEN invalid_text_representation THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Malformed input', 'data', NULL);
    END;

    IF v_old_hash IS NULL OR v_new_hash IS NULL OR v_new_expires IS NULL THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'Missing fields', 'data', NULL);
    END IF;

    SELECT * INTO v_token_row
    FROM identity.refresh_tokens
    WHERE token_hash = v_old_hash
      AND revoked_at IS NULL
      AND expires_at > now()
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED',
                                  'message', 'Invalid or expired refresh token', 'data', NULL);
    END IF;

    UPDATE identity.refresh_tokens
    SET revoked_at = now(), replaced_by_token_hash = v_new_hash
    WHERE id = v_token_row.id;

    INSERT INTO identity.refresh_tokens (user_id, token_hash, expires_at)
    VALUES (v_token_row.user_id, v_new_hash, v_new_expires);

    SELECT * INTO v_user FROM identity.users WHERE id = v_token_row.user_id;
    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', false, 'code', 'UNAUTHENTICATED', 'message', 'User no longer exists', 'data', NULL);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', jsonb_build_object(
        'id', v_user.id, 'email', v_user.email, 'display_name', v_user.display_name
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_rotate_refresh_token failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
