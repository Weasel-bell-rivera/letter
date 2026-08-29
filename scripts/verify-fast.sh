#!/usr/bin/env bash

set -u

EXPECTED_UNITY_VERSION="6000.5.8f1"

pass_count=0
warn_count=0
fail_count=0

pass() {
    pass_count=$((pass_count + 1))
    printf 'PASS  %-12s %s\n' "$1" "$2"
}

warn() {
    warn_count=$((warn_count + 1))
    printf 'WARN  %-12s %s\n' "$1" "$2"
}

fail() {
    fail_count=$((fail_count + 1))
    printf 'FAIL  %-12s %s\n' "$1" "$2"
}

if ! command -v git >/dev/null 2>&1; then
    printf 'ERROR git is required.\n' >&2
    exit 2
fi

project_root="$(git rev-parse --show-toplevel 2>/dev/null)" || {
    printf 'ERROR run this script inside the Git repository.\n' >&2
    exit 2
}
cd "$project_root" || exit 2

printf 'W1 fast verification\n'
printf 'Project: %s\n\n' "$project_root"

# Text conflict markers are always unsafe to hand to Unity or commit.
conflict_output="$(rg -n --hidden \
    --glob '!.git/**' \
    --glob '!Library/**' \
    --glob '!Temp/**' \
    --glob '!Logs/**' \
    --glob '!obj/**' \
    '^(<<<<<<< .+|>>>>>>> .+)$' . 2>/dev/null || true)"
if [ -n "$conflict_output" ]; then
    fail "TEXT-001" "Merge-conflict markers found:"
    printf '%s\n' "$conflict_output"
else
    pass "TEXT-001" "No merge-conflict markers found."
fi

# The project rules pin the Unity editor version.
version_file="ProjectSettings/ProjectVersion.txt"
if [ ! -f "$version_file" ]; then
    fail "UNITY-001" "$version_file is missing."
else
    actual_version="$(sed -n 's/^m_EditorVersion: //p' "$version_file" | head -n 1)"
    if [ "$actual_version" = "$EXPECTED_UNITY_VERSION" ]; then
        pass "UNITY-001" "Unity version is $actual_version."
    else
        fail "UNITY-001" "Expected $EXPECTED_UNITY_VERSION, found ${actual_version:-unknown}."
    fi
fi

# Generated Unity directories must never be tracked by Git.
tracked_generated="$(git ls-files | awk '
    BEGIN { IGNORECASE = 1 }
    /^(Library|Temp|Obj|Build|Builds|Logs|UserSettings|MemoryCaptures|Recordings)\// { print }
')"
if [ -n "$tracked_generated" ]; then
    fail "GIT-001" "Generated Unity paths are tracked:"
    printf '%s\n' "$tracked_generated"
else
    pass "GIT-001" "No generated Unity directories are tracked."
fi

# Every Unity asset or directory below Assets needs a sibling .meta file.
missing_meta=0
while IFS= read -r asset_path; do
    [ -n "$asset_path" ] || continue
    case "$asset_path" in
        *.meta) continue ;;
    esac
    if [ ! -e "${asset_path}.meta" ]; then
        if [ "$missing_meta" -eq 0 ]; then
            fail "META-001" "Assets without matching .meta files:"
        fi
        printf '%s\n' "$asset_path"
        missing_meta=$((missing_meta + 1))
    fi
done < <(find Assets -mindepth 1 \( -type f -o -type d \) \
    ! -name '.DS_Store' \
    ! -path 'Assets/Screenshots' ! -path 'Assets/Screenshots/*' | sort)
if [ "$missing_meta" -eq 0 ]; then
    pass "META-001" "Every asset and asset directory has a matching .meta file."
fi

orphan_meta=0
while IFS= read -r meta_path; do
    [ -n "$meta_path" ] || continue
    asset_path="${meta_path%.meta}"
    if [ ! -e "$asset_path" ]; then
        if [ "$orphan_meta" -eq 0 ]; then
            fail "META-002" "Orphaned .meta files:"
        fi
        printf '%s\n' "$meta_path"
        orphan_meta=$((orphan_meta + 1))
    fi
done < <(find Assets -type f -name '*.meta' ! -path 'Assets/Screenshots.meta' | sort)
if [ "$orphan_meta" -eq 0 ]; then
    pass "META-002" "No orphaned .meta files found."
fi

# Assembly definitions are JSON and can be validated without starting Unity.
if ! command -v python3 >/dev/null 2>&1; then
    warn "ASMDEF-001" "python3 is unavailable; skipped asmdef JSON validation."
else
    invalid_asmdef=0
    while IFS= read -r asmdef_path; do
        [ -n "$asmdef_path" ] || continue
        if ! python3 -m json.tool "$asmdef_path" >/dev/null 2>&1; then
            if [ "$invalid_asmdef" -eq 0 ]; then
                fail "ASMDEF-001" "Invalid asmdef JSON files:"
            fi
            printf '%s\n' "$asmdef_path"
            invalid_asmdef=$((invalid_asmdef + 1))
        fi
    done < <(find Assets -type f -name '*.asmdef' | sort)
    if [ "$invalid_asmdef" -eq 0 ]; then
        pass "ASMDEF-001" "All asmdef files contain valid JSON."
    fi
fi

# A formal room Scene must have its authoritative room document and registrations.
room_errors=0
room_scene_count=0
while IFS= read -r scene_path; do
    [ -n "$scene_path" ] || continue
    room_scene_count=$((room_scene_count + 1))

    region_name="$(basename "$(dirname "$scene_path")")"
    scene_name="$(basename "$scene_path" .unity)"
    region_lower="$(printf '%s' "$region_name" | tr '[:upper:]' '[:lower:]')"
    room_id="$(printf '%s' "$scene_name" | tr '[:lower:]' '[:upper:]')"
    room_doc="docs/rooms/${region_lower}/${room_id}.md"
    room_index="docs/rooms/${region_lower}/ROOM_INDEX.md"

    if [ ! -f "$room_doc" ]; then
        fail "ROOM-001" "$scene_path has no matching $room_doc."
        room_errors=$((room_errors + 1))
    fi
    if [ ! -f "$room_index" ] || ! grep -Fq "$room_id" "$room_index"; then
        fail "ROOM-002" "$room_id is not registered in $room_index."
        room_errors=$((room_errors + 1))
    fi
    if [ ! -f "docs/maps/MAP.md" ] || ! grep -Fq "$room_id" docs/maps/MAP.md; then
        fail "ROOM-003" "$room_id is not registered in docs/maps/MAP.md."
        room_errors=$((room_errors + 1))
    fi
done < <(find Assets/Scenes/Levels -mindepth 2 -maxdepth 2 -type f -name '*.unity' | sort)
if [ "$room_errors" -eq 0 ]; then
    pass "ROOM-ALL" "$room_scene_count formal room Scenes have matching documents and registrations."
fi

printf '\nSummary: %d passed, %d warnings, %d failed.\n' \
    "$pass_count" "$warn_count" "$fail_count"
printf 'Unity was not started; compilation, runtime behavior, PlayMode, and visuals were not verified.\n'

if [ "$fail_count" -gt 0 ]; then
    exit 1
fi
exit 0
