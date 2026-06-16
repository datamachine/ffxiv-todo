#!/usr/bin/env bash

set -euo pipefail

usage() {
  echo "Usage: $0 --tag vX.Y.Z --input <input-dir> --output <output-dir>" >&2
  exit 1
}

fail() {
  echo "$1" >&2
  exit 1
}

make_absolute_path() {
  case "$1" in
    /*) printf '%s\n' "$1" ;;
    *) printf '%s/%s\n' "$(pwd)" "$1" ;;
  esac
}

tag=""
input_dir=""
output_dir=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)
      [[ $# -ge 2 ]] || usage
      tag="$2"
      shift 2
      ;;
    --input)
      [[ $# -ge 2 ]] || usage
      input_dir="$2"
      shift 2
      ;;
    --output)
      [[ $# -ge 2 ]] || usage
      output_dir="$2"
      shift 2
      ;;
    *)
      usage
      ;;
  esac
done

[[ -n "$tag" && -n "$input_dir" && -n "$output_dir" ]] || usage
input_dir="$(make_absolute_path "$input_dir")"
output_dir="$(make_absolute_path "$output_dir")"
[[ "$tag" =~ ^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]] || fail "Tag must match vX.Y.Z without zero-padded segments."

archive_tool=""
if command -v zip >/dev/null 2>&1; then
  archive_tool="zip"
elif command -v bsdtar >/dev/null 2>&1; then
  archive_tool="bsdtar"
else
  fail "zip or bsdtar is required to create release archives."
fi

version="${tag#v}"

required_files=(
  "FfxivTodo.dll"
  "FfxivTodo.deps.json"
  "FfxivTodo.json"
)

for required_file in "${required_files[@]}"; do
  [[ -f "$input_dir/$required_file" ]] || fail "Missing required file: $input_dir/$required_file"
done

mkdir -p "$output_dir"

stage_dir="$(mktemp -d)"
trap 'rm -rf "$stage_dir"' EXIT

for required_file in "${required_files[@]}"; do
  cp "$input_dir/$required_file" "$stage_dir/$required_file"
done

staged_manifest="$stage_dir/FfxivTodo.json"
sed -E "s/\"AssemblyVersion\"[[:space:]]*:[[:space:]]*\"[^\"]*\"/\"AssemblyVersion\": \"$version\"/" \
  "$staged_manifest" > "$stage_dir/FfxivTodo.json.tmp"
mv "$stage_dir/FfxivTodo.json.tmp" "$staged_manifest"
grep -q "\"AssemblyVersion\": \"$version\"" "$staged_manifest" || fail "Failed to stamp AssemblyVersion in $staged_manifest"

zip_path="$output_dir/FfxivTodo-$version.zip"
rm -f "$zip_path"
(
  cd "$stage_dir"
  if [[ "$archive_tool" == "zip" ]]; then
    zip -q "$zip_path" FfxivTodo.dll FfxivTodo.deps.json FfxivTodo.json
  else
    bsdtar -a -cf "$zip_path" FfxivTodo.dll FfxivTodo.deps.json FfxivTodo.json
  fi
)

echo "Created $zip_path"
