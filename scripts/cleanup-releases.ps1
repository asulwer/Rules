# Delete old orphaned GitHub Releases (v1.0.x) that no longer have matching tags
# Usage: .\cleanup-releases.ps1 -Token "ghp_***"
param(
    [Parameter(Mandatory=$true)]
    [string]$Token
)

$ErrorActionPreference = "Stop"

$owner = "asulwer"
$repo = "RoslynRules"

# Date-based tags that should be kept
$validTags = @(
    "2026.5.31", "2026.5.31-1", "2026.6.1",
    "2026.6.2", "2026.6.2-1", "2026.6.2-2", "2026.6.3"
)

Write-Host "Fetching all releases..." -ForegroundColor Cyan

# Get all releases (paginated)
$releases = @()
$page = 1
do {
    $pageReleases = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases?per_page=100&page=$page" `
        -Headers @{ Authorization = "***" }
    $releases += $pageReleases
    $page++
} while ($pageReleases.Count -eq 100)

Write-Host "Found $($releases.Count) total releases" -ForegroundColor Yellow
Write-Host ""

# Find releases to delete (those not matching valid date-based tags)
$orphanedReleases = $releases | Where-Object { $validTags -notcontains $_.tag_name }

if ($orphanedReleases.Count -eq 0) {
    Write-Host "No orphaned releases found. All clean!" -ForegroundColor Green
    exit 0
}

Write-Host "Orphaned releases to delete:" -ForegroundColor Red
foreach ($rel in $orphanedReleases) {
    Write-Host "  - $($rel.tag_name) (ID: $($rel.id))" -ForegroundColor Red
}

Write-Host ""
$confirm = Read-Host "Delete $($orphanedReleases.Count) orphaned releases? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Aborted." -ForegroundColor Gray
    exit 0
}

foreach ($rel in $orphanedReleases) {
    Write-Host "Deleting release $($rel.tag_name)..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases/$($rel.id)" `
            -Method Delete `
            -Headers @{ Authorization = "***" }
        Write-Host "  Deleted" -ForegroundColor Green
    } catch {
        Write-Host "  Failed: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Done! Orphaned releases cleaned up." -ForegroundColor Green
