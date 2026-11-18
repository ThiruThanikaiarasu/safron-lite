#!/bin/bash
# Creates and seeds the Saffron databases. Runs once per `docker compose up`;
# skips if the catalog already exists unless SAFFRON_RESET=1.
set -euo pipefail

SQLCMD=(/opt/mssql-tools/bin/sqlcmd -S "${SAFFRON_DB_HOST:-sql}" -U sa -P "$MSSQL_SA_PASSWORD" -b -I)

if [ "${SAFFRON_RESET:-0}" != "1" ]; then
    present=$("${SQLCMD[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'SaffronCatalog'")
    if [ "$(echo "$present" | tr -d '[:space:]')" = "1" ]; then
        echo "init: databases present, nothing to do"
        exit 0
    fi
fi

echo "init: resetting databases"
"${SQLCMD[@]}" -v AppPassword="$SAFFRON_APP_PASSWORD" -i /db/00_reset.sql

for t in Alder Birch; do
    db="Saffron_$t"
    lc=$(echo "$t" | tr '[:upper:]' '[:lower:]')
    echo "init: $db"
    "${SQLCMD[@]}" -v TenantDb="$db" -i /db/10_tenant_schema.sql
    "${SQLCMD[@]}" -v TenantDb="$db" -i /db/11_tenant_procs.sql
    "${SQLCMD[@]}" -d "$db" -i "/db/seed/seed_${lc}.sql"
done

# catalog last: its presence means the tenants are ready
"${SQLCMD[@]}" -i /db/01_catalog.sql
echo "init: done"
