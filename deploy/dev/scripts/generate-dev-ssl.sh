#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SSL_DIR="$(cd "$SCRIPT_DIR/../nginx/ssl" && pwd)"
mkdir -p "$SSL_DIR"
if [[ -f "$SSL_DIR/dev.crt" && -f "$SSL_DIR/dev.key" ]]; then
  echo "SSL dev já existe em $SSL_DIR"
  exit 0
fi
echo "Gerando certificado autoassinado em $SSL_DIR ..."
openssl req -x509 -nodes -days 825 -newkey rsa:2048 \
  -keyout "$SSL_DIR/dev.key" \
  -out "$SSL_DIR/dev.crt" \
  -subj "/CN=10.0.0.79/O=LioConecta Dev/C=BR" \
  -addext "subjectAltName=IP:10.0.0.79,DNS:localhost"
chmod 644 "$SSL_DIR/dev.crt"
chmod 600 "$SSL_DIR/dev.key"
echo "Certificado dev criado."
