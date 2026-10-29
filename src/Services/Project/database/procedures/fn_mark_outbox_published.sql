CREATE OR REPLACE FUNCTION project.fn_mark_outbox_published(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
BEGIN
    IF p_actor_role <> 'system' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Internal operation only', 'data', NULL);
    END IF;

    UPDATE project.outbox_events
    SET published_at = now()
    WHERE id IN (SELECT (jsonb_array_elements_text(p_payload->'ids'))::uuid);

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', 'Marked', 'data', NULL);

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_mark_outbox_published failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
