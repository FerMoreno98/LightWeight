#!/usr/bin/env bash
# Deploys one commit on the server. CD runs it over SSH; it can also be run by hand:
#   /opt/lightweight/deploy/deploy.sh <sha of 40 characters>
#
# GitHub's key is tied to this script in ~/.ssh/authorized_keys (forced command), so the SHA
# arrives in SSH_ORIGINAL_COMMAND instead of as an argument.
#
# Steps:
#   0. Check that the commit is on main and pull its images from ghcr.io (nothing changed yet)
#   1. Copy of the database (migrations are the only thing hard to undo)
#   2. The repo goes to that commit: docker-compose.prod.yaml and the Caddyfile match the images
#   3. IMAGE_TAG=<sha> in .env, so a later "docker compose up" by hand keeps this version
#   4. Restart what changed
#   5. Check that the API and the web stay up; if not, show the logs and fail (CD goes red)
#
# Everything is inside main(): bash reads a script as it runs it, and step 2 can change this very
# file. A function is read whole before it starts, so the change only applies to the next deploy.
set -euo pipefail

main() {
  local sha="${SSH_ORIGINAL_COMMAND:-${1:-}}"
  if [[ ! "$sha" =~ ^[0-9a-f]{40}$ ]]; then
    echo "Uso: deploy.sh <sha del commit, 40 caracteres>" >&2
    exit 2
  fi

  local root
  root="$(cd "$(dirname "$(readlink -f "$0")")/.." && pwd)"
  cd "$root"
  local compose=(docker compose -f docker-compose.prod.yaml)

  # Two deploys at the same time would step on each other
  exec 9>"/tmp/lightweight-deploy.lock"
  if ! flock -n 9; then
    echo "Ya hay un despliegue en marcha" >&2
    exit 1
  fi

  echo "==> Desplegando $sha"

  # Before touching anything: the commit must be on main and its images must exist.
  # A wrong SHA fails here and leaves the server as it was
  git fetch --quiet origin
  if ! git merge-base --is-ancestor "$sha" origin/main 2>/dev/null; then
    echo "!! $sha no es un commit de main" >&2
    exit 1
  fi
  echo "==> Descargando imágenes"
  IMAGE_TAG="$sha" "${compose[@]}" pull --quiet api front

  # 1. Copy of the database. Not on the first deploy, when there is no database yet.
  #    The last 10 are kept. They stay on this server: they are not the daily backup.
  mkdir -p backups
  if [ -n "$("${compose[@]}" ps -q --status running db 2>/dev/null)" ]; then
    local file
    file="backups/pre-deploy-$(date +%Y%m%d-%H%M%S)-${sha:0:7}.sql.gz"
    echo "==> Copia de la BD en $file"
    "${compose[@]}" exec -T db pg_dump -U postgres lightweight | gzip > "$file.tmp"
    mv "$file.tmp" "$file"
    ls -1t backups/pre-deploy-*.sql.gz | tail -n +11 | xargs -r rm --
  fi

  # 2. The code of that commit. Fails if someone edited tracked files on the server
  #    (.env and backups/ are not tracked, they are not touched)
  echo "==> Código del commit"
  git checkout --quiet --detach "$sha"

  # 3. The version, in .env
  sed -i '/^#\?[[:space:]]*IMAGE_TAG=/d' .env
  echo "IMAGE_TAG=$sha" >> .env

  # 4. Restart what changed (the images were pulled above). Only api and front change version;
  #    caddy and db only when their image line in docker-compose.prod.yaml changes
  echo "==> Arrancando"
  "${compose[@]}" up -d --remove-orphans

  # 5. A broken API (migration that fails, missing variable) dies at startup and Docker keeps
  #    restarting it. If after 30 s it is still running with no restarts, it is fine
  echo "==> Comprobando"
  sleep 30
  local service id state failed=0
  for service in api front caddy; do
    id="$("${compose[@]}" ps -q "$service")"
    state="$(docker inspect -f '{{.State.Status}} {{.RestartCount}}' "$id" 2>/dev/null || echo "missing")"
    # caddy is usually not recreated, so its restart count may come from before: only its status
    if [ "$service" = caddy ] && [ "${state%% *}" = running ]; then
      state="running 0"
    fi
    if [ "$state" != "running 0" ]; then
      echo "!! $service no está bien ($state)" >&2
      "${compose[@]}" logs --tail 50 "$service" >&2
      failed=1
    fi
  done
  if [ "$failed" -ne 0 ]; then
    echo "!! Despliegue fallido. Para volver a la versión anterior: deploy.sh <sha anterior>" >&2
    exit 1
  fi

  # Old images: each version is ~300 MB, they would fill the disk
  docker image prune -f > /dev/null

  echo "==> Desplegado $sha"
}

main "$@"
exit
