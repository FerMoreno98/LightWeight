#!/usr/bin/env bash
# Daily copy of the database, encrypted, to storage outside the server. Run by the systemd timer
# deploy/systemd/lightweight-backup.timer; it can also be run by hand:
#   /opt/lightweight/deploy/backup.sh
#
#   pg_dump ──► gzip ──► age (public key) ──► rclone ──► BACKUP_REMOTE/lightweight-<date>.sql.gz.age
#
# - Encrypted with the PUBLIC key only. The private key is not on the server (it lives in the
#   password manager), so whoever gets into the storage account, or into this server, cannot
#   read the copies already uploaded.
# - How long copies are kept is decided by the bucket (lifecycle rule), not by this script.
# - If BACKUP_PING_URL is set (healthchecks.io), it is pinged on success and on failure. When a
#   day goes by without a ping, healthchecks.io sends an email: a copy that fails is noticed.
#
# Settings, in .env (next to docker-compose.prod.yaml):
#   BACKUP_REMOTE         rclone remote and path, e.g. b2:lightweight-backups
#   BACKUP_AGE_RECIPIENT  age public key (age1...)
#   BACKUP_PING_URL       optional, healthchecks.io URL
set -Eeuo pipefail

root="$(cd "$(dirname "$(readlink -f "$0")")/.." && pwd)"
cd "$root"

# Reads one value from .env without running it as shell code (a value with $ would break "source")
conf() {
  grep -E "^$1=" .env | tail -n 1 | cut -d= -f2- | sed -e "s/^['\"]//" -e "s/['\"]\$//"
}

remote="$(conf BACKUP_REMOTE || true)"
recipient="$(conf BACKUP_AGE_RECIPIENT || true)"
ping="$(conf BACKUP_PING_URL || true)"

ping_url() {
  if [ -n "$ping" ]; then
    curl -fsS -m 10 --retry 3 "$ping$1" > /dev/null || true
  fi
}
trap 'echo "!! Copia fallida" >&2; ping_url /fail' ERR

if [ -z "$remote" ] || [[ "$recipient" != age1* ]]; then
  echo "!! Faltan BACKUP_REMOTE o BACKUP_AGE_RECIPIENT (age1...) en .env" >&2
  false
fi

ping_url /start

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
file="lightweight-$(date -u +%Y%m%d-%H%M%S).sql.gz.age"

echo "==> Copia de la BD"
docker compose -f docker-compose.prod.yaml exec -T db pg_dump -U postgres lightweight \
  | gzip \
  | age -r "$recipient" > "$tmp/$file"

# An empty database still gives a few KB; less means pg_dump wrote nothing
size="$(stat -c %s "$tmp/$file")"
if [ "$size" -lt 1024 ]; then
  echo "!! La copia ocupa $size bytes: algo ha ido mal" >&2
  false
fi

echo "==> Subiendo $file ($((size / 1024)) KB) a $remote"
rclone copyto "$tmp/$file" "$remote/$file"

ping_url ""
echo "==> Copia hecha: $file"
