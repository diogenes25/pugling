#!/usr/bin/env bash
# Rollengang an einer Kopie der echten DB (pm-loop Step 2).
#
# Warum: Die Söhne lernen auf backend/Pugling.Api/pugling.db, der Heimnetz-Server hält sie auf :5200. Ein Rollengang
# dort schriebe Malus, Logins und Lernstand in ihre echten Daten. Eine Wegwerf-DB (Seed, E2E) zeigt dagegen nur, was
# ein frisch angelegtes Kind erlebt: In Runde 138 (2026-11-24) standen drei von vier Funden nur in den echten Daten
# („etwa 0 Karten je Tag“, ungehörte Kartenmeldungen, 21 von 209 Wörtern).
#
# Was es tut: Kopie per SQLite-Backup (WAL eingeschlossen, die Quelle nur lesend), auf der Kopie bekommen alle Konten
# eine Test-PIN, dann startet die API (Release, Seed aus, Login-Bremse aus) auf 127.0.0.1:<port> gegen die Kopie und
# spielt dabei ausstehende Migrationen ein – das ist zugleich die Migrationsprobe an einer Kopie.
#
#   bash .claude/scripts/kopie-server.sh [zielordner] [port]      # im Hintergrund starten; Ende mit Strg+C / TaskStop
#
# Anmeldung danach: Kind über auth/child (rechnet ab wie die Sohn-App), Erwachsene über auth/login, PIN aus PUGLING_KOPIE_PIN.
# Bleibt der Prozess nach dem Stoppen am Port hängen: Pugling.Api.exe auf dem Port beenden.
set -euo pipefail

root="${CLAUDE_PROJECT_DIR:-$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)}"
ziel="${1:-${TMPDIR:-/tmp}/pugling-kopie}"
port="${2:-5201}"
pin="${PUGLING_KOPIE_PIN:-4711}"
[ "$port" = "5200" ] && { echo "Port 5200 gehört dem Heimnetz-Server – anderen Port wählen." >&2; exit 1; }

mkdir -p "$ziel"
kopie="$ziel/kopie.db"
rm -f "$kopie" "$kopie-wal" "$kopie-shm"

python -I - "$root/backend/Pugling.Api/pugling.db" "$kopie" "$pin" <<'PY'
import base64, hashlib, os, sqlite3, sys
quelle, kopie, pin = sys.argv[1:4]
src = sqlite3.connect(f"file:{quelle}?mode=ro", uri=True)
dst = sqlite3.connect(kopie)
src.backup(dst)
src.close()
# Format von PinHasher.Hash: pbkdf2.<iterationen>.<salt>.<schlüssel>, SHA-256, 32 Byte.
salt = os.urandom(16)
key = hashlib.pbkdf2_hmac("sha256", pin.encode(), salt, 100_000, 32)
dst.execute("UPDATE Accounts SET PinHash = ?",
            (".".join(["pbkdf2", "100000", base64.b64encode(salt).decode(), base64.b64encode(key).decode()]),))
dst.commit()
konten = dst.execute("SELECT a.Id, a.DisplayName, group_concat(p.Role) FROM Accounts a "
                     "JOIN AccountProfiles p ON p.AccountId = a.Id GROUP BY a.Id ORDER BY a.Id").fetchall()
dst.close()
for k in konten:
    print(f"Konto {k[0]}: {k[1]} ({k[2]})")
PY

echo "Kopie: $kopie · API: http://127.0.0.1:$port/api/v1"
cd "$root/backend/Pugling.Api"
exec env ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="http://127.0.0.1:$port" \
  "ConnectionStrings__Default=Data Source=$kopie" Jwt__Key="pm-walk-local-only-key-$(date +%s)-0123456789abcdef" \
  Seed__Enabled=false RateLimiting__LoginEnabled=false \
  dotnet run -c Release --no-launch-profile
