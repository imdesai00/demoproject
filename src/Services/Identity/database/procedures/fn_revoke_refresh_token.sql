-- identity.fn_revoke_refresh_token(p_actor_id, p_actor_role, {token_hash}) — logout.
-- Idempotent: revoking an already-revoked or unknown token still returns ok=true, since the
-- end state the caller wants ("this token no longer works") is already true.
CREATE OR REPLACE FUNCTION identity.fn_revoke_refresh_token(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, pg_temp
AS $$
DECLARE
    v_token_hash text;
BEGIN
    v_token_hash := NULLIF(p_payload->>'token_hash', '');
    IF v_token_hash IS NULL THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'token_hash is required', 'data', NULL);
    END IF;

    UPDATE identity.refresh_tokens
    SET revoked_at = now()
    WHERE token_hash = v_token_hash AND revoked_at IS NULL;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Revoked', 'data', NULL);

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_revoke_refresh_token failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
