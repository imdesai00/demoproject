#!/bin/bash
# Runs once, on first container start (docker-entrypoint-initdb.d convention). Creates one
# database per service and applies that service's migrations -> procedures -> grants, in order,
# using each service's own database/ folder (mounted read-only at /service-db/<name>).
set -euo pipefail

for svc in identity project task; do
  db="${svc}_db"
  echo "=== $db ==="

  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -d postgres -c "CREATE DATABASE ${db};"

  for f in /service-db/${svc}/migrations/[0-9][0-9][0-9]_*.sql; do
    [ -f "$f" ] || continue
    case "$(basename "$f")" in
      999_*) continue ;; # grants run last, after the schema's functions exist
    esac
    echo "  applying $(basename "$f")"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -d "$db" -f "$f"
  done

  for f in /service-db/${svc}/procedures/*.sql; do
    [ -f "$f" ] || continue
    echo "  applying procedures/$(basename "$f")"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -d "$db" -f "$f"
  done

  for f in /service-db/${svc}/migrations/999_*.sql; do
    [ -f "$f" ] || continue
    echo "  applying $(basename "$f")"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -d "$db" -f "$f"
  done
done
