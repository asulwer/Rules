#!/usr/bin/env bash
# Create GitHub Releases for all date-based tags
# Requires GITHUB_TOKEN environment variable with repo scope

set -e

OWNER="asulwer"
REPO="RoslynRules"

if [ -z "$GITHUB_TOKEN" ]; then
    echo "ERROR: GITHUB_TOKEN environment variable not set."
    echo "Create a token at: https://github.com/settings/tokens"
    echo "Required scopes: repo (or public_repo for public repos)"
    exit 1
fi

# Array of tags and their release notes
declare -a TAGS=(
    "2026.5.31"
    "2026.5.31-1"
    "2026.6.1"
    "2026.6.2"
    "2026.6.2-1"
    "2026.6.2-2"
    "2026.6.3"
)

echo "Creating GitHub Releases for $OWNER/$REPO..."
echo ""

for TAG in "${TAGS[@]}"; do
    echo "Creating release for tag: $TAG"
    
    # Check if release already exists
    EXISTING=$(curl -s -H "Authorization: token $GITHUB_TOKEN" \
        "https://api.github.com/repos/$OWNER/$REPO/releases/tags/$TAG" | grep -o '"id":' || true)
    
    if [ ! -z "$EXISTING" ]; then
        echo "  Release for $TAG already exists. Skipping."
        continue
    fi
    
    # Get the tag message (annotation) for release notes
    BODY=$(git tag -l --format='%(contents)' "$TAG" 2>/dev/null || echo "Release $TAG")
    
    # Create the release
    RESPONSE=$(curl -s -X POST \
        -H "Authorization: token $GITHUB_TOKEN" \
        -H "Accept: application/vnd.github.v3+json" \
        "https://api.github.com/repos/$OWNER/$REPO/releases" \
        -d "{
            \"tag_name\": \"$TAG\",
            \"name\": \"$TAG\",
            \"body\": \"$BODY\",
            \"draft\": false,
            \"prerelease\": false
        }" 2>&1)
    
    if echo "$RESPONSE" | grep -q '"id":'; then
        echo "  ✓ Created release for $TAG"
    else
        echo "  ✗ Failed to create release for $TAG"
        echo "  Response: $RESPONSE"
    fi
    
    echo ""
done

echo "Done!"
echo "View releases at: https://github.com/$OWNER/$REPO/releases"
