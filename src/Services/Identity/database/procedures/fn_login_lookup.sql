-- identity.fn_login_lookup(p_actor_id, p_actor_role, {email})
-- Anonymous endpoint. Returns the user row (including password_hash) so C# can verify it with
-- BCrypt — Postgres never does the comparison. Always returns ok=true; "not found" comes back
-- as data=NULL so the API gives the same generic "invalid email or password" either way,
-- without leaking which part was wrong.
CREATE OR REPLACE FUNCTION identity.fn_login_lookup(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, pg_temp
AS $$
DECLARE
    v_email text;
    v_row   identity.users%ROWTYPE;
BEGIN
    v_email := NULLIF(lower(btrim(p_payload->>'email')), '');
    IF v_email IS NULL THEN
        RETURN jsonb_build_object('ok', false, 'code', 'VALIDATION', 'message', 'email is required', 'data', NULL);
    END IF;

    SELECT * INTO v_row FROM identity.users WHERE email = v_email;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', NULL);
    END IF;

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', jsonb_build_object(
        'id', v_row.id, 'email', v_row.email, 'password_hash', v_row.password_hash, 'display_name', v_row.display_name
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_login_lookup failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
