#!/bin/bash
set -e

cat >> "$PGDATA/postgresql.conf" <<EOF
wal_level = replica
max_wal_senders = 4
max_replication_slots = 4
hot_standby = on
EOF

cat >> "$PGDATA/pg_hba.conf" <<EOF
host replication replicator 0.0.0.0/0 md5
EOF

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator_pass';
    SELECT pg_create_physical_replication_slot('slave_1_slot');
    SELECT pg_create_physical_replication_slot('slave_2_slot');
EOSQL

pg_ctl reload -D "$PGDATA"
