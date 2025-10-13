#!/bin/bash
# post-commit hook for .NET app version bump
# Works with semantic versioning (1.1.0) and [minor]/[major] flags

set -e

# Move to repo root
cd "$(git rev-parse --show-toplevel)" || exit 1

CSPROJ_FILE="src/SubtitleTools/SubtitleTools.csproj"
#PROGRAM_FILE="src/SubtitleTools/Program.cs"
README_FILE="README.md"

# Extract version from csproj (first <Version> tag)
CURRENT_VERSION=$(grep -oPm1 '(?<=<Version>)[^<]+' "$CSPROJ_FILE")

if [ -z "$CURRENT_VERSION" ]; then
  echo "Could not find <Version> tag in $CSPROJ_FILE"
  exit 1
fi

IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Determine bump type
COMMIT_MSG=$(git log -1 --pretty=%B)

if [[ "$COMMIT_MSG" == *"[major]"* ]]; then
  MAJOR=$((MAJOR + 1))
  MINOR=0
  PATCH=0
  BUMP_TYPE="major"
elif [[ "$COMMIT_MSG" == *"[minor]"* ]]; then
  MINOR=$((MINOR + 1))
  PATCH=0
  BUMP_TYPE="minor"
else
  PATCH=$((PATCH + 1))
  BUMP_TYPE="patch"
fi

NEW_VERSION="$MAJOR.$MINOR.$PATCH"

echo "🔢 Version bumped ($BUMP_TYPE): $CURRENT_VERSION → $NEW_VERSION"

# Update all places
sed -i.bak "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$CSPROJ_FILE"
sed -i.bak "s/<AssemblyVersion>$CURRENT_VERSION<\/AssemblyVersion>/<AssemblyVersion>$NEW_VERSION<\/AssemblyVersion>/" "$CSPROJ_FILE"
#sed -i.bak "s/SetVersion(\"$CURRENT_VERSION\")/SetVersion(\"$NEW_VERSION\")/" "$PROGRAM_FILE"
sed -i.bak "s/Current version: $CURRENT_VERSION/Current version: $NEW_VERSION/" "$README_FILE"

rm -f "$CSPROJ_FILE.bak" "$PROGRAM_FILE.bak" "$README_FILE.bak"

# Stage and amend commit
git add "$CSPROJ_FILE" "$PROGRAM_FILE" "$README_FILE"
git commit --amend --no-edit