param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string[]]$RevitVersions = @("2025", "2026"),
    [string]$RevitInstallRootBase = "$env:ProgramW6432\Autodesk"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "ExcelExporterImporter\ExcelExporterImporter.csproj"
$extensionBin = Join-Path $repoRoot "axis\ExcelExporterImporter.extension\bin"

if (!(Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

$failedVersions = @()
$skippedVersions = @()

foreach ($version in $RevitVersions) {
    Write-Host "Building Revit $version ($Configuration)..." -ForegroundColor Cyan

    $revitInstallRoot = Join-Path $RevitInstallRootBase "Revit $version"
    if (!(Test-Path (Join-Path $revitInstallRoot "RevitAPI.dll"))) {
        Write-Warning "Skipping Revit $version. RevitAPI.dll not found at $revitInstallRoot"
        $skippedVersions += $version
        continue
    }

    dotnet build $projectPath -c $Configuration -p:RevitVersion=$version -p:RevitInstallRoot="$revitInstallRoot" -p:IncludeLegacyAddin=false -p:DeployToRevitAddins=false
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Build failed for Revit $version"
        $failedVersions += $version
        continue
    }

    $sourceDir = Join-Path $repoRoot "ExcelExporterImporter\bin\$Configuration\Revit$version"
    $targetDir = Join-Path $extensionBin "Revit$version"

    if (!(Test-Path $sourceDir)) {
        Write-Warning "Build output not found for Revit $version at $sourceDir"
        $failedVersions += $version
        continue
    }

    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Recurse -Force
}

if ($skippedVersions.Count -gt 0) {
    Write-Warning "Skipped versions: $($skippedVersions -join ', ')"
}

if ($failedVersions.Count -gt 0) {
    throw "Build failed for versions: $($failedVersions -join ', ')"
}

Write-Host "Hybrid axis extension payload staged under: $extensionBin" -ForegroundColor Green
