#!/usr/bin/env pwsh
# Gate de cobertura del dominio (spec 020, FR-009/FR-010, SC-001).
# Ejecuta la suite del dominio con recolección de cobertura y FALLA (exit 1)
# si la cobertura de líneas del proyecto de dominio cae por debajo del umbral.
#
# Uso:
#   pwsh backend/Tests/Monolegal.Domain.Tests/verify-coverage.ps1
#   pwsh backend/Tests/Monolegal.Domain.Tests/verify-coverage.ps1 -Threshold 0.85
#   pwsh backend/Tests/Monolegal.Domain.Tests/verify-coverage.ps1 -Html          # genera reporte HTML
#   pwsh backend/Tests/Monolegal.Domain.Tests/verify-coverage.ps1 -Html -Open     # genera y lo abre
#
# Pensado para el gate de CI (Constitución, Principio IV: cobertura >=85%, publicada por PR).

param(
    [double]$Threshold = 0.85,
    [switch]$Html,
    [switch]$Open
)

$ErrorActionPreference = 'Stop'

# Raíz del repo (tres niveles arriba de backend/Tests/Monolegal.Domain.Tests).
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$projectDir = $PSScriptRoot
$proj = Join-Path $projectDir 'Monolegal.Domain.Tests.csproj'
$resultsDir = Join-Path $projectDir 'TestResults'
$runsettings = Join-Path $projectDir 'coverlet.runsettings'
$reportDir = Join-Path $repoRoot 'coverage-report'

Write-Host "==> Ejecutando suite del dominio con cobertura..." -ForegroundColor Cyan
dotnet test $proj --collect:"XPlat Code Coverage" --settings $runsettings --results-directory $resultsDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "La suite del dominio falló (pruebas en rojo). Gate de cobertura abortado."
    exit 1
}

$cov = Get-ChildItem $resultsDir -Recurse -Filter 'coverage.cobertura.xml' |
       Sort-Object LastWriteTime | Select-Object -Last 1
if (-not $cov) {
    Write-Error "No se encontró coverage.cobertura.xml. ¿Se generó la cobertura?"
    exit 1
}

[xml]$report = Get-Content $cov.FullName
$lineRate = [double]$report.coverage.'line-rate'

$pct = '{0:P2}' -f $lineRate
$thr = '{0:P2}' -f $Threshold
if ($lineRate -lt $Threshold) {
    Write-Error "Cobertura de líneas del dominio $pct < umbral $thr. Gate FALLIDO."
    exit 1
}

Write-Host "Cobertura de líneas del dominio: $pct (umbral $thr). Gate OK." -ForegroundColor Green

if ($Html) {
    Write-Host "==> Generando reporte HTML..." -ForegroundColor Cyan
    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        Write-Error "reportgenerator no está instalado. Ejecuta: dotnet tool install -g dotnet-reportgenerator-globaltool"
        exit 1
    }
    reportgenerator -reports:"$($cov.FullName)" -targetdir:"$reportDir" -reporttypes:Html
    $index = Join-Path $reportDir 'index.html'
    Write-Host "Reporte HTML: $index" -ForegroundColor Green
    if ($Open) { Start-Process $index }
}

exit 0
