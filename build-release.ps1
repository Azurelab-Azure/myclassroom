# ============================================================
# Build & Release: publish + Inno Setup
# ============================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
$root = "G:\csharft\CourseApp"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  My Schedule - Release Build" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# ---------- [1/5] Kill old process ----------
Write-Host ""
Write-Host "[1/5] Killing old process..." -ForegroundColor Yellow
try { taskkill /F /IM CourseApp.exe 2>&1 | Out-Null } catch { }
try { Get-Process CourseApp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch { }

# ---------- [2/5] Clean ----------
Write-Host "[2/5] Cleaning bin / obj / publish..." -ForegroundColor Yellow
Set-Location $root
Remove-Item bin, obj, publish -Recurse -Force -ErrorAction SilentlyContinue

# ---------- [3/5] Publish ----------
Write-Host "[3/5] dotnet publish (self-contained)..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -p:DebugType=none `
    -o "$root\publish"

if (-not (Test-Path "$root\publish\CourseApp.exe")) {
    Write-Host "publish failed! $root\publish\CourseApp.exe not found" -ForegroundColor Red
    exit 1
}
Write-Host "  publish OK" -ForegroundColor Green

# ---------- [4/5] Inno Setup ----------
Write-Host "[4/5] Inno Setup packaging..." -ForegroundColor Yellow

$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    Write-Host "Inno Setup not found: $iscc" -ForegroundColor Red
    Write-Host "Install: winget install JRSoftware.InnoSetup" -ForegroundColor Yellow
    exit 1
}

& $iscc "$root\installer\CourseApp.iss"

# ---------- [5/5] Check ----------
$setup = "$root\installer-output\CourseApp-Setup-1.1.0.exe"
if (Test-Path $setup) {
    $size = (Get-Item $setup).Length / 1MB
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Release Done" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Installer: $setup" -ForegroundColor Green
    Write-Host ("  Size:      {0:N2} MB" -f $size) -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Packaging failed! $setup not found" -ForegroundColor Red
    exit 1
}