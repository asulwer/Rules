# Create GitHub Releases for all date-based tags
# Requires GITHUB_TOKEN environment variable with repo scope
# Run: $env:GITHUB_TOKEN="***"; .\create-releases.ps1

$ErrorActionPreference = "Stop"

$owner = "asulwer"
$repo = "RoslynRules"

if (-not $env:GITHUB_TOKEN) {
    Write-Host "ERROR: GITHUB_TOKEN environment variable not set." -ForegroundColor Red
    Write-Host "Create a token at: https://github.com/settings/tokens"
    Write-Host "Required scopes: repo (or public_repo for public repos)"
    exit 1
}

$tags = @(
    "2026.5.31"
    "2026.5.31-1"
    "2026.6.1"
    "2026.6.2"
    "2026.6.2-1"
    "2026.6.2-2"
)

Write-Host "Creating GitHub Releases for $owner/$repo..." -ForegroundColor Cyan
Write-Host ""

foreach ($tag in $tags) {
    Write-Host "Creating release for tag: $tag" -ForegroundColor Yellow

    # Check if release already exists
    try {
        $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases/tags/$tag" `
            -Headers @{ Authorization = "token $env:GITHUB_TOKEN" } `
            -ErrorAction Stop
        Write-Host "  Release for $tag already exists. Skipping." -ForegroundColor Gray
        continue
    } catch {
        # Release doesn't exist, continue
    }

    # Create the release
    $body = @{
        tag_name = $tag
        name = $tag
        draft = $false
        prerelease = $false
        generate_release_notes = $true
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases" `
            -Method Post `
            -Headers @{
                Authorization = "token $env:GITHUB_TOKEN"
                Accept = "application/vnd.github.v3+json"
            } `
            -ContentType "application/json" `
            -Body $body

        Write-Host "  Created release for $tag" -ForegroundColor Green
    } catch {
        Write-Host "  Failed to create release for $tag" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
    }

    Write-Host ""
}

Write-Host "Done!" -ForegroundColor Cyan
Write-Host "View releases at: https://github.com/$owner/$repo/releases"
