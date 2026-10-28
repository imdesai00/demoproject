-- Applied last (after all procedures/*.sql have created the schema's functions): grants the
-- service role EXECUTE on everything that exists in the schema right now.
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA identity TO svc_identity;
