#!/usr/bin/env bash
set -euo pipefail

solution_file="${1:-All.slnx}"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

if [[ ! -f "$solution_file" ]]; then
  echo "Solution file '$solution_file' not found in $script_dir." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI not found. Install the .NET SDK and try again." >&2
  exit 1
fi

mapfile -t projects < <(find . -type f -name '*.csproj' \
  ! -path '*/bin/*' \
  ! -path '*/obj/*' \
  ! -path '*/packages/*' \
  ! -path '*/.vs/*' \
  | sort)

if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No project files found to add to '$solution_file'."
  exit 0
fi

printf 'Adding %s project(s) to solution "%s"...\n' "${#projects[@]}" "$solution_file"
for project_path in "${projects[@]}"; do
  project_path="${project_path#./}"
  printf '  Adding %s\n' "$project_path"
  dotnet sln "$solution_file" add "$project_path"
done

printf 'All projects added to "%s".\n' "$solution_file"
