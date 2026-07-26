#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <prototypes|releases> <game-slug> [unity-editor-version]"
  echo "Example: $0 prototypes stack-jump 6000.3.20f1"
}

if [[ $# -lt 2 || $# -gt 3 ]]; then
  usage
  exit 1
fi

bucket="$1"
slug="$2"
unity_version="${3:-6000.3.20f1}"

if [[ "$bucket" != "prototypes" && "$bucket" != "releases" ]]; then
  echo "Bucket must be either prototypes or releases."
  exit 1
fi

if [[ ! "$slug" =~ ^[a-z0-9]+(-[a-z0-9]+)*$ ]]; then
  echo "Game slug must be kebab-case, for example: stack-jump"
  exit 1
fi

project_dir="$bucket/$slug"

if [[ -e "$project_dir" ]]; then
  echo "Project already exists: $project_dir"
  exit 1
fi

title="$(echo "$slug" | awk -F- '{ for (i=1; i<=NF; i++) { $i=toupper(substr($i,1,1)) substr($i,2) } print }' OFS=' ')"
namespace="$(echo "$slug" | awk -F- '{ for (i=1; i<=NF; i++) { printf "%s", toupper(substr($i,1,1)) substr($i,2) } }')"

if [[ "$namespace" =~ ^[0-9] ]]; then
  namespace="Game$namespace"
fi

package_slug="${slug//-}"

if [[ "$package_slug" =~ ^[0-9] ]]; then
  package_slug="game$package_slug"
fi

mkdir -p \
  "$project_dir/Assets/_Project/Art" \
  "$project_dir/Assets/_Project/Audio" \
  "$project_dir/Assets/_Project/Prefabs" \
  "$project_dir/Assets/_Project/Scenes" \
  "$project_dir/Assets/_Project/Scripts" \
  "$project_dir/Assets/_Project/Settings" \
  "$project_dir/Packages" \
  "$project_dir/ProjectSettings"

touch \
  "$project_dir/Assets/_Project/Art/.gitkeep" \
  "$project_dir/Assets/_Project/Audio/.gitkeep" \
  "$project_dir/Assets/_Project/Prefabs/.gitkeep" \
  "$project_dir/Assets/_Project/Scenes/.gitkeep" \
  "$project_dir/Assets/_Project/Scripts/.gitkeep" \
  "$project_dir/Assets/_Project/Settings/.gitkeep" \
  "$project_dir/Packages/.gitkeep"

cat > "$project_dir/README.md" <<EOF
# $title

Unity hyper-casual game project.

## Project

- Unity editor: $unity_version
- Platform: Android
- Package name: com.mannlab.games.$package_slug
- Namespace: MannLab.Games.$namespace

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
EOF

cat > "$project_dir/ProjectSettings/ProjectVersion.txt" <<EOF
m_EditorVersion: $unity_version
m_EditorVersionWithRevision: $unity_version
EOF

cat > "$project_dir/Assets/_Project/Scripts/${namespace}Game.asmdef" <<EOF
{
  "name": "MannLab.Games.$namespace",
  "rootNamespace": "MannLab.Games.$namespace",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
EOF

echo "Created Unity project shell: $project_dir"
