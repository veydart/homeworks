#!/bin/bash
set -e

until PGPASSWORD=root pg_isready -h pg-master -U postgres; do
  echo "Waiting for master..."
  sleep 2
done

rm -rf /var/lib/postgresql/data/*

PGPASSWORD=replicator_pass pg_basebackup \
  -h pg-master -U replicator -D /var/lib/postgresql/data \
  -Fp -Xs -P -R \
  -S slave_1_slot

cat >> /var/lib/postgresql/data/postgresql.conf <<EOF
hot_standby = on
primary_conninfo = 'host=pg-master port=5432 user=replicator password=replicator_pass'
primary_slot_name = 'slave_1_slot'
EOF

pg_ctl start -D /var/lib/postgresql/data -l /var/lib/postgresql/logfile
tail -f /var/lib/postgresql/logfile
