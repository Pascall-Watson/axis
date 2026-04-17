param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string[]]$RevitVersions = @("2024", "2025", "2026", "2027"),
    [string]$RevitInstallRootBase = "$env:ProgramW6432\Autodesk"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot "ExcelExporterImporter"
$extensionBin = Join-Path $repoRoot "axis\axis.extension\bin"

# Map versions to their target frameworks
$versionInfo = @{
    "2024" = @{ Framework = "net481" }
    "2025" = @{ Framework = "net8.0-windows" }
    "2026" = @{ Framework = "net8.0-windows" }
    "2027" = @{ Framework = "net10.0-windows" }
}

$failedVersions = @()
$skippedVersions = @()

foreach ($version in $RevitVersions) {
    Write-Host "Building Revit $version ($Configuration)..." -ForegroundColor Cyan

    # Use version-specific .csproj file
    $versionProjectPath = Join-Path $projectDir "ExcelExporterImporter.$version.csproj"
    if (!(Test-Path $versionProjectPath)) {
        Write-Warning "Skipping Revit $version. Project file not found: $versionProjectPath"
        $skippedVersions += $version
        continue
    }

    $revitInstallRoot = Join-Path $RevitInstallRootBase "Revit $version"
    if (!(Test-Path (Join-Path $revitInstallRoot "RevitAPI.dll"))) {
        Write-Warning "Skipping Revit $version. RevitAPI.dll not found at $revitInstallRoot"
        $skippedVersions += $version
        continue
    }

    # Build version-specific project
    dotnet build $versionProjectPath -c $Configuration -p:RevitInstallRoot="$revitInstallRoot" -p:DeployToRevitAddins=false
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Build failed for Revit $version"
        $failedVersions += $version
        continue
    }

    # DLLs are placed in framework subdirectories by the SDK
    $framework = $versionInfo[$version].Framework
    $sourceDir = Join-Path $projectDir "bin\$Configuration\Revit$version\$framework"
    $targetDir = Join-Path $extensionBin "Revit$version"

    if (!(Test-Path $sourceDir)) {
        Write-Warning "Build output not found for Revit $version at $sourceDir"
        $failedVersions += $version
        continue
    }

    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Recurse -Force
    Write-Host "Staged Revit $version binaries to $targetDir" -ForegroundColor Green
}

if ($skippedVersions.Count -gt 0) {
    Write-Warning "Skipped versions: $($skippedVersions -join ', ')"
}

if ($failedVersions.Count -gt 0) {
    throw "Build failed for versions: $($failedVersions -join ', ')"
}

Write-Host "Hybrid axis extension payload staged under: $extensionBin" -ForegroundColor Green
