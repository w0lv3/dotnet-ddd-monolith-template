#!/usr/bin/env sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
root_dir=$(dirname -- "$script_dir")
api_project="$root_dir/src/Example.Api/Example.Api.csproj"
output_dir="$root_dir/openapi"
output_file="$output_dir/openapi.yaml"
port=${OPENAPI_PORT:-5076}
url="http://127.0.0.1:$port/openapi/v1.yaml"
check=false

if [ "${1:-}" = "--check" ]; then
    check=true
elif [ "$#" -ne 0 ]; then
    printf 'Usage: %s [--check]\n' "$0" >&2
    exit 2
fi

mkdir -p "$output_dir"
generated_file=$(mktemp "$output_dir/.openapi.yaml.XXXXXX")
log_file=$(mktemp "${TMPDIR:-/tmp}/generate-openapi.XXXXXX")
api_pid=""

cleanup() {
    if [ -n "$api_pid" ]; then
        kill "$api_pid" 2>/dev/null || true
        wait "$api_pid" 2>/dev/null || true
    fi

    rm -f "$generated_file" "$log_file"
}

trap cleanup EXIT HUP INT TERM

dotnet build "$api_project"

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="http://127.0.0.1:$port" \
    dotnet run --project "$api_project" --no-build --no-launch-profile \
    >"$log_file" 2>&1 &
api_pid=$!

attempt=0
until curl --fail --silent "$url" --output "$generated_file" 2>/dev/null; do
    attempt=$((attempt + 1))

    if ! kill -0 "$api_pid" 2>/dev/null || [ "$attempt" -ge 60 ]; then
        printf 'OpenAPI generation failed. API output:\n' >&2
        cat "$log_file" >&2
        exit 1
    fi

    sleep 0.25
done

if [ "$check" = true ]; then
    if [ ! -f "$output_file" ]; then
        printf '%s does not exist. Run scripts/generate-openapi.sh first.\n' "$output_file" >&2
        exit 1
    fi

    if ! cmp -s "$output_file" "$generated_file"; then
        printf '%s is stale. Regenerate it with scripts/generate-openapi.sh.\n' "$output_file" >&2
        diff -u "$output_file" "$generated_file" || true
        exit 1
    fi

    printf '%s is current.\n' "$output_file"
else
    chmod 644 "$generated_file"
    mv "$generated_file" "$output_file"
    generated_file=""
    printf 'Generated %s.\n' "$output_file"
fi
