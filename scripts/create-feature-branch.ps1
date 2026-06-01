#Requires -Version 7.0
<#
.SYNOPSIS
    Create a feature branch for a GitHub issue and open a PR.

.DESCRIPTION
    Creates a feature branch named after the issue, pushes it, and opens a PR
    that references the issue for automatic closure on merge.

.PARAMETER IssueNumber
    GitHub issue number to work on.

.PARAMETER Token
    GitHub personal access token (classic) with repo scope.

.PARAMETER RepoOwner
    Repository owner (default: asulwer)

.PARAMETER RepoName
    Repository name (default: RoslynRules)

.EXAMPLE
    .\create-feature-branch.ps1 -IssueNumber 42 -Token "ghp_xx…xxxx"
#>
param(
    [Parameter(Mandatory)]
    [int]$IssueNumber,

    [Parameter(Mandatory)]
    [string]$Token,

    [string]$RepoOwner = "asulwer",
    [string]$RepoName = "RoslynRules"
)

$ErrorActionPreference = "Stop"
$headers = @{
    Authorization = "***"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}
$baseUrl = "https://api.github.com/repos/$RepoOwner/$RepoName"

# Fetch issue details
Write-Host "Fetching issue #$IssueNumber..." -ForegroundColor Cyan
$issue = Invoke-RestMethod -Uri "$baseUrl/issues/$IssueNumber" -Headers $headers

$issueTitle = $issue.title
$issueBody = $issue.body
$branchName = "feature/issue-$IssueNumber-$($issueTitle.ToLower() -replace '[^a-z0-9]+', '-')"
$branchName = $branchName.Trim('-').Substring(0, [Math]::Min(50, $branchName.Length))

Write-Host "Issue: $issueTitle" -ForegroundColor White
Write-Host "Branch: $branchName" -ForegroundColor White

# Create branch from latest master
Write-Host "`nCreating branch $branchName from master..." -ForegroundColor Cyan

# Get latest master SHA
$masterRef = Invoke-RestMethod -Uri "$baseUrl/git/ref/heads/master" -Headers $headers
$masterSha = $masterRef.object.sha

# Create branch reference
$branchBody = @{
    ref = "refs/heads/$branchName"
    sha = $masterSha
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/git/refs" -Method POST -Headers $headers -Body $branchBody | Out-Null
Write-Host "  Branch created" -ForegroundColor Green

# Create PR
Write-Host "`nCreating pull request..." -ForegroundColor Cyan
$prBody = @"
Closes #$IssueNumber

## Summary
$issueTitle

## Related Issue
#$IssueNumber
"@

$prData = @{
    title = $issueTitle
    body = $prBody
    head = $branchName
    base = "master"
} | ConvertTo-Json -Depth 10

$pr = Invoke-RestMethod -Uri "$baseUrl/pulls" -Method POST -Headers $headers -Body $prData
Write-Host "  PR created: $($pr.html_url)" -ForegroundColor Green

Write-Host "`nDone!" -ForegroundColor Cyan
Write-Host "Branch:  $branchName"
Write-Host "PR:      $($pr.html_url)"
Write-Host "Issue:   $($issue.html_url)"
