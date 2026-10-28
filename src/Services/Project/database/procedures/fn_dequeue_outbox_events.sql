-- Internal-only: called by Project.Api's own OutboxPublisherHostedService (actor role 'system',
-- never reachable from an HTTP request). FOR UPDATE SKIP LOCKED makes this safe even if the
-- service ever runs multiple replicas polling concurrently.
CREATE OR REPLACE FUNCTION project.fn_dequeue_outbox_events(
    p_actor_id   uuid,
    p_actor_role text,
    p_payload    jsonb
) RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = project, pg_temp
AS $$
DECLARE
    v_batch_size int;
BEGIN
    IF p_actor_role <> 'system' THEN
        RETURN jsonb_build_object('ok', false, 'code', 'FORBIDDEN', 'message', 'Internal operation only', 'data', NULL);
    END IF;

    v_batch_size := COALESCE((p_payload->>'batch_size')::int, 20);

    RETURN jsonb_build_object('ok', true, 'code', 'OK', 'message', NULL, 'data', (
        SELECT COALESCE(jsonb_agg(jsonb_build_object(
            'id', id, 'event_type', event_type, 'routing_key', routing_key, 'payload', payload
        ) ORDER BY created_at), '[]'::jsonb)
        FROM (
            SELECT id, event_type, routing_key, payload, created_at
            FROM project.outbox_events
            WHERE published_at IS NULL
            ORDER BY created_at
            LIMIT v_batch_size
            FOR UPDATE SKIP LOCKED
        ) batch
    ));

EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'fn_dequeue_outbox_events failed: % (%)', SQLERRM, SQLSTATE;
        RETURN jsonb_build_object('ok', false, 'code', 'INTERNAL', 'message', 'Unexpected error', 'data', NULL);
END;
$$;
