DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'svc_task') THEN
        CREATE ROLE svc_task LOGIN PASSWORD 'svc_task_dev_password';
    END IF;
END
$$;

REVOKE ALL ON ALL TABLES IN SCHEMA task FROM svc_task;
GRANT USAGE ON SCHEMA task TO svc_task;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA task FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA task REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER ROLE svc_task SET statement_timeout = '5s';
ALTER ROLE svc_task SET idle_in_transaction_session_timeout = '10s';
