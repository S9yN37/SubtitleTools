#!/bin/bash
# pre-commit hook for .NET app version bump
set -e

cd "$(git rev-parse --show-toplevel)" || exit 1

CSPROJ_FILE="src/SubtitleTools/SubtitleTools.csproj"
README_FILE="README.md"

# Extract current version
CURRENT_VERSION=$(grep -m1 '<Version>' "$CSPROJ_FILE" | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/')

if [ -z "$CURRENT_VERSION" ]; then
  echo "Could not find <Version> tag in $CSPROJ_FILE"
  exit 1
fi

IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Check commit message from .git/COMMIT_EDITMSG if it exists, otherwise use a default
if [ -f .git/COMMIT_EDITMSG ]; then
  COMMIT_MSG=$(cat .git/COMMIT_EDITMSG)
else
  # For amended commits, we just do patch bump
  COMMIT_MSG=""
fi

# Determine bump type
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

# Update files
sed -i.bak "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$CSPROJ_FILE"
sed -i.bak "s/<AssemblyVersion>$CURRENT_VERSION<\/AssemblyVersion>/<AssemblyVersion>$NEW_VERSION<\/AssemblyVersion>/" "$CSPROJ_FILE"
sed -i.bak "s/Current version: $CURRENT_VERSION/Current version: $NEW_VERSION/" "$README_FILE"

rm -f "$CSPROJ_FILE.bak" "$README_FILE.bak"

# Stage the updated files
git add "$CSPROJ_FILE" "$README_FILE"

exit 0