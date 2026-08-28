#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$repository_root"

dotnet tool restore
dotnet restore Example.slnx
dotnet ef database update \
  --project src/Example.Infrastructure/Example.Infrastructure.csproj \
  --startup-project src/Example.Infrastructure/Example.Infrastructure.csproj
