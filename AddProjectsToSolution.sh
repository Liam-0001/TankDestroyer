#!/usr/bin/env bash
set -euo pipefail

solution_file="${1:-All.slnx}"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

if [[ ! -f "$solution_file" ]]; then
  echo "Solution file '$solution_file' not found. Creating a new one..." >&2
  dotnet new sln -n "${solution_file%.*}"
fi

mapfile -t projects < <(find . -type f -name '*.csproj' \
  ! -path '*/bin/*' \
  ! -path '*/obj/*' \
  ! -path '*/packages/*' \
  ! -path '*/.vs/*' \
  | sort)

if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No project files found."
  exit 0
fi

printf 'Adding %s project(s) to solution "%s"...\n' "${#projects[@]}" "$solution_file"
# In je loop waar je projecten toevoegt:
for project_path in "${projects[@]}"; do
  # Voeg toe aan solution
  dotnet sln "$solution_file" add "$project_path"

  # Als het een bot is, dwing de referentie naar de API
  if [[ "$project_path" == *"Bots"* ]]; then
    echo "Fixing references for $project_path"
    # Gebruik het relatieve pad naar je API project
    dotnet add "$project_path" reference Source/TankDestroyer.API/TankDestroyer.API.csproj || true
  fi
done



printf 'Restoring dependencies for the entire solution...\n'
dotnet restore "$solution_file"

printf 'All projects added and dependencies restored in "%s".\n' "$solution_file"