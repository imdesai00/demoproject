-- Least-privilege DB role for Identity.Api: EXECUTE on identity's functions only, no table
-- grants, no superuser. The password here is a local-dev default — override it via
-- POSTGRES_IDENTITY_PASSWORD in production and rotate it outside of version control.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'svc_identity') THEN
        CREATE ROLE svc_identity LOGIN PASSWORD 'svc_identity_dev_password';
    END IF;
END
$$;

REVOKE ALL ON ALL TABLES IN SCHEMA identity FROM svc_identity;
GRANT USAGE ON SCHEMA identity TO svc_identity;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA identity FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA identity REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER ROLE svc_identity SET statement_timeout = '5s';
ALTER ROLE svc_identity SET idle_in_transaction_session_timeout = '10s';
