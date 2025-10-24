#!/usr/bin/env bash
set -euo pipefail

DEFAULT_CHANNEL="release/9.0.1xx-preview"
SDK_VERSION="${DOTNET_SDK_VERSION:-}"
CHANNEL="${DOTNET_INSTALL_CHANNEL:-$DEFAULT_CHANNEL}"
INSTALL_DIR="${DOTNET_ROOT:-$HOME/.dotnet}"
DOTNET_BIN_DIR="$INSTALL_DIR"
FORCE=0
RUN_INFO=0

print_help() {
  cat <<'USAGE'
install-dotnet.sh [-f|--force] [--info] [--help]

Installs the .NET SDK required to build this repository using Microsoft's
official dotnet-install script. By default the script installs the latest
preview SDK from the 9.0 channel. Override the version or channel with the
DOTNET_SDK_VERSION or DOTNET_INSTALL_CHANNEL environment variables.

Options:
  -f, --force   Always run the installer even when a compatible SDK is found
  --info        Run 'dotnet --info' after installation
  -h, --help    Show this help message
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -f|--force)
      FORCE=1
      shift
      ;;
    --info)
      RUN_INFO=1
      shift
      ;;
    -h|--help)
      print_help
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      print_help >&2
      exit 1
      ;;
  esac
done

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Error: required command '$1' not found in PATH" >&2
    exit 1
  fi
}

log() {
  printf '\n[%s] %s\n' "$(date '+%H:%M:%S')" "$1"
}

case "$(uname -s)" in
  Linux)
    OS_NAME="Linux"
    ;;
  Darwin)
    OS_NAME="macOS"
    ;;
  *)
    echo "Unsupported operating system: $(uname -s)" >&2
    exit 1
    ;;
esac

require_command curl
require_command tar

sdk_already_installed() {
  if ! command -v dotnet >/dev/null 2>&1; then
    return 1
  fi

  local target_pattern
  if [[ -n "$SDK_VERSION" ]]; then
    target_pattern="^${SDK_VERSION//./\\.}"
  else
    target_pattern='^9\\.0\\.'
  fi

  if dotnet --list-sdks 2>/dev/null | grep -E "$target_pattern" >/dev/null; then
    return 0
  fi
  return 1
}

if [[ $FORCE -eq 0 ]] && sdk_already_installed; then
  log ".NET SDK already installed. Use --force to reinstall."
  exit 0
fi

log "Installing .NET SDK on $OS_NAME"
mkdir -p "$INSTALL_DIR"

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT
INSTALL_SCRIPT="$TMP_DIR/dotnet-install.sh"

log "Downloading dotnet-install script"
curl -sSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"
chmod +x "$INSTALL_SCRIPT"

INSTALL_ARGS=("--install-dir" "$INSTALL_DIR" "--no-path")
if [[ -n "$SDK_VERSION" ]]; then
  INSTALL_ARGS+=("--version" "$SDK_VERSION")
else
  INSTALL_ARGS+=("--channel" "$CHANNEL")
fi

log "Running installer with args: ${INSTALL_ARGS[*]}"
"$INSTALL_SCRIPT" "${INSTALL_ARGS[@]}"

log ".NET SDK installation completed"

if ! command -v dotnet >/dev/null 2>&1; then
  export DOTNET_ROOT="$INSTALL_DIR"
  export PATH="$DOTNET_ROOT:$PATH"
fi

if [[ ":$PATH:" != *":$DOTNET_BIN_DIR:"* ]]; then
  cat <<EOM

Add the following lines to your shell profile to use the installed SDK:
  export DOTNET_ROOT="$INSTALL_DIR"
  export PATH="\$DOTNET_ROOT:\$PATH"
EOM
fi

if [[ $RUN_INFO -eq 1 ]]; then
  log "dotnet --info"
  "$DOTNET_BIN_DIR/dotnet" --info
fi

log "Done"
