#!/usr/bin/env pwsh

<#
.SYNOPSIS
Builds the DesktopGL Alpha targets and runs the engine test suite.

.DESCRIPTION
Builds only the maintained Windows/DesktopGL projects. Mobile targets, Web,
Multiplatform, GISM, and the whole solution are deliberately excluded because
they need out-of-scope workloads or are isolated experiments.
#>

param(
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testResultsDirectory = Join-Path (Join-Path $repoRoot 'artifacts') 'test-results'
New-Item -ItemType Directory -Path $testResultsDirectory -Force | Out-Null

$projects = @(
    'Engine/Engine.Core.csproj',
    'Engine.Content/Engine.Content.csproj',
    'Editor/Engine.Editor.csproj',
    'Engine.Desktop/Engine.Desktop.csproj'
)

foreach($project in $projects)
{
    $projectPath = Join-Path $repoRoot $project
    Write-Host "==> Building $project ($Configuration)"
    dotnet build $projectPath -c $Configuration --nologo
    if($LASTEXITCODE -ne 0)
    {
        throw "Build failed for $project with exit code $LASTEXITCODE."
    }
}

Write-Host '==> Running Engine.Tests'
dotnet test (Join-Path $repoRoot 'Engine.Tests/Engine.Tests.csproj') `
    -c $Configuration `
    --nologo `
    --results-directory $testResultsDirectory `
    --logger 'trx;LogFileName=Engine.Tests.trx'
if($LASTEXITCODE -ne 0)
{
    throw "Tests failed with exit code $LASTEXITCODE."
}

Write-Host "Build and test complete. TRX results: $testResultsDirectory"
