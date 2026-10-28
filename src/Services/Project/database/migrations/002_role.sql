DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'svc_project') THEN
        CREATE ROLE svc_project LOGIN PASSWORD 'svc_project_dev_password';
    END IF;
END
$$;

REVOKE ALL ON ALL TABLES IN SCHEMA project FROM svc_project;
GRANT USAGE ON SCHEMA project TO svc_project;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA project FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA project REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER ROLE svc_project SET statement_timeout = '5s';
ALTER ROLE svc_project SET idle_in_transaction_session_timeout = '10s';
