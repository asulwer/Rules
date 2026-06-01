#Requires -Version 7.0
<#
.SYNOPSIS
    Create GitHub issues from ROADMAP.md for the RoslynRules repository.

.DESCRIPTION
    Creates labels and issues based on the current ROADMAP.md items.
    Requires a GitHub personal access token with `repo` scope.

.PARAMETER Token
    GitHub personal access token (classic) with repo scope.

.PARAMETER RepoOwner
    Repository owner (default: asulwer)

.PARAMETER RepoName
    Repository name (default: RoslynRules)

.EXAMPLE
    .\create-roadmap-issues.ps1 -Token "ghp_xxxxxxxxxxxxxxxxxxxx"
#>
param(
    [Parameter(Mandatory)]
    [string]$Token,

    [string]$RepoOwner = "asulwer",
    [string]$RepoName = "RoslynRules"
)

$ErrorActionPreference = "Stop"
$headers = @{
    Authorization = "Bearer $Token"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}
$baseUrl = "https://api.github.com/repos/$RepoOwner/$RepoName"

function Invoke-GitHubApi {
    param([string]$Uri, [string]$Method = "GET", [object]$Body = $null)
    $params = @{ Uri = $Uri; Method = $Method; Headers = $headers }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
    $response = Invoke-RestMethod @params
    return $response
}

Write-Host "Creating labels..." -ForegroundColor Cyan

$labels = @(
    @{ name = "roadmap"; color = "0052CC"; description = "Item from project roadmap" },
    @{ name = "priority:high"; color = "B60205"; description = "High value / low effort" },
    @{ name = "priority:medium"; color = "FBCA04"; description = "Medium value" },
    @{ name = "priority:nice-to-have"; color = "0E8A16"; description = "Nice to have" },
    @{ name = "status:blocked"; color = "D93F0B"; description = "Blocked or deferred" },
    @{ name = "status:excluded"; color = "666666"; description = "Won't implement" }
)

foreach ($label in $labels) {
    try {
        Invoke-GitHubApi -Uri "$baseUrl/labels" -Method "POST" -Body $label | Out-Null
        Write-Host "  Created label: $($label.name)" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 422) {
            Write-Host "  Label already exists: $($label.name)" -ForegroundColor Yellow
        } else {
            throw
        }
    }
}

Write-Host "`nCreating issues..." -ForegroundColor Cyan

$issues = @(
    @{
        title = "Rule composition/template system"
        body = @"
Support placeholders like ``{entity}.Age >= {minAge}`` for reusable rule templates.

This would allow consumers to define parameterized rules that can be instantiated with specific values at compile time.

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:medium")
    },
    @{
        title = "Built-in rule predicates library"
        body = @"
Add common predicates for frequent validation patterns:

- ``Rule.IsNotNull()``
- ``Rule.GreaterThan(minValue)``
- ``Rule.LessThan(maxValue)``
- ``Rule.InRange(min, max)``
- ``Rule.MatchesRegex(pattern)``
- etc.

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:medium")
    },
    @{
        title = "Localizable rule descriptions (i18n)"
        body = @"
Support multiple languages for rule descriptions to enable global applications.

Potential approaches:
- Resource file (.resx) integration
- Custom ``IStringLocalizer`` support
- Description key + translation dictionary

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "RuleResult as readonly record struct"
        body = @"
Convert ``RuleResult`` from mutable ``struct`` to ``readonly record struct`` to reduce boxing and improve immutability.

Current struct uses nullable reference types, which causes boxing. A ``readonly record struct`` would eliminate this while maintaining value semantics.

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "Replace reflection-based JSON Id restoration"
        body = @"
Current JSON loader uses reflection to restore ``RuleId`` from serialized data. Find a cleaner, non-reflection approach.

Options:
- Source generators
- Custom JSON converter with explicit Id mapping
- Deserialization constructor

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "AssemblyCompiler assembly reference filtering"
        body = @"
Currently loads ALL loaded assemblies as references. Allow explicit filtering to reduce compilation overhead and improve AOT compatibility.

Options:
- Explicit assembly list parameter
- Assembly whitelist/blacklist
- Auto-detect required assemblies from expression

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "Rule dependency graph visualization"
        body = @"
Generate Graphviz DOT output from ``DependsOnRuleId`` relationships for visual inspection and documentation.

Example output:
```dot
digraph Rules {
    RuleA -> RuleB;
    RuleB -> RuleC;
}
```

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "Rule metrics (eval count, avg time, failure rate)"
        body = @"
Track per-rule execution statistics over time:

- Total evaluation count
- Average execution time
- Failure rate (percentage)
- Last execution timestamp

This would enable monitoring dashboards and performance optimization.

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    },
    @{
        title = "Rule result caching (memoization)"
        body = @"
Cache rule results by parameter hash to avoid redundant evaluation of expensive rules.

Requirements:
- Opt-in per-rule (not global)
- Configurable cache duration
- Thread-safe implementation
- Cache invalidation strategy

See [ROADMAP.md](ROADMAP.md).
"@
        labels = @("roadmap", "priority:nice-to-have")
    }
)

$created = 0
foreach ($issue in $issues) {
    try {
        Invoke-GitHubApi -Uri "$baseUrl/issues" -Method "POST" -Body $issue | Out-Null
        Write-Host "  Created issue: $($issue.title)" -ForegroundColor Green
        $created++
    } catch {
        if ($_.Exception.Response.StatusCode -eq 422) {
            Write-Host "  Issue may already exist: $($issue.title)" -ForegroundColor Yellow
        } else {
            Write-Host "  Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "`nDone! Created $created issues." -ForegroundColor Cyan
