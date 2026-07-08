# =============================================================
# setup-git.ps1  —  Run ONCE on every machine that clones the repo
# Registers UnityYAMLMerge as the Git merge driver for Unity files
# so scenes, prefabs, and assets merge cleanly without broken refs.
#
# Usage (PowerShell — run from repo root):
#   .\setup-git.ps1
# =============================================================

$UnityVersion = "6000.3.8f1"

# --- 1. Auto-detect Unity path from this project's Library ---
$mergeTool = $null

$editorInstanceFile = Join-Path $PSScriptRoot "Library\EditorInstance.json"
if (Test-Path $editorInstanceFile) {
    try {
        $info = Get-Content $editorInstanceFile | ConvertFrom-Json
        $dataPath = $info.app_contents_path   # e.g. D:/…/Editor/Data
        if ($dataPath) {
            $candidate = Join-Path ($dataPath.Replace("/", "\")) "Tools\UnityYAMLMerge.exe"
            if (Test-Path $candidate) { $mergeTool = $candidate }
        }
    } catch { }
}

# --- 2. Fallback: common install locations ---
if (-not $mergeTool) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Data\Tools\UnityYAMLMerge.exe",
        "C:\Program Files (x86)\Unity\Hub\Editor\$UnityVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { $mergeTool = $p; break }
    }
}

# --- 3. Ask user if still not found ---
if (-not $mergeTool) {
    Write-Host ""
    Write-Host "Could not find UnityYAMLMerge.exe automatically." -ForegroundColor Yellow
    Write-Host "Open Unity Hub > Installs > $UnityVersion > locate Editor\Data\Tools\UnityYAMLMerge.exe"
    Write-Host "Enter the full path (or press Enter to abort):" -ForegroundColor Yellow
    $mergeTool = (Read-Host "Path").Trim('"')
    if ([string]::IsNullOrWhiteSpace($mergeTool)) {
        Write-Host "Aborted." -ForegroundColor Red; exit 1
    }
}

if (-not (Test-Path $mergeTool)) {
    Write-Host "File not found: $mergeTool" -ForegroundColor Red; exit 1
}

# Forward slashes — required by git
$mergeToolFwd = $mergeTool.Replace("\", "/")

# --- 4. Write merge driver directly into the global .gitconfig ---
# (using git config CLI for paths with spaces causes truncation)
$gitconfigPath = git config --global --list --show-origin 2>$null |
    Select-String "^file:(.+?)\t" |
    ForEach-Object { $_.Matches[0].Groups[1].Value } |
    Select-Object -First 1

if (-not $gitconfigPath -or -not (Test-Path $gitconfigPath)) {
    $gitconfigPath = "$env:USERPROFILE\.gitconfig"
}

$block = @"

[merge "unityyamlmerge"]
	name = Unity SmartMerge
	driver = \"$mergeToolFwd\" merge -p %O %B %A %A
	trustExitCode = false
"@

$existing = Get-Content $gitconfigPath -Raw -ErrorAction SilentlyContinue

# Remove any previous unityyamlmerge block before re-adding
$cleaned = $existing -replace '(?ms)\[merge "unityyamlmerge"\][^\[]*', ''

Set-Content -Path $gitconfigPath -Value ($cleaned.TrimEnd() + $block) -Encoding UTF8

Write-Host ""
Write-Host "Done! Git is now configured to use UnityYAMLMerge." -ForegroundColor Green
Write-Host "  Tool       : $mergeTool"                           -ForegroundColor Cyan
Write-Host "  Config     : $gitconfigPath"                       -ForegroundColor Cyan
Write-Host ""
Write-Host "You only need to run this script ONCE per machine."  -ForegroundColor DarkGray
