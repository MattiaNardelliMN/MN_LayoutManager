<#
.SYNOPSIS
    Compila il plugin "Gestione Layout" e lo installa per AutoCAD 2024.

.DESCRIPTION
    Fa tre cose, in quest'ordine:
      1. compila il progetto in Release ed esegue i test;
      2. se qualcosa fallisce SI FERMA e non installa niente;
      3. copia il plugin in un "bundle" dentro
         %AppData%\Autodesk\ApplicationPlugins\, cosi' AutoCAD lo carica da solo
         a ogni avvio, senza dover digitare NETLOAD.

    IMPORTANTE: AutoCAD deve essere CHIUSO durante l'installazione, altrimenti
    i file del plugin risultano bloccati e la copia fallisce.

.PARAMETER SkipTests
    Salta i test automatici (sconsigliato: servono a verificare che la logica funzioni).

.PARAMETER Uninstall
    Rimuove il plugin installato invece di installarlo.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------- costanti
$AutoCadYear      = '2024'
$AutoCadSeries    = 'R24.3'   # sigla interna di AutoCAD 2024
$BundleName       = 'MN_LayoutManager.bundle'
$PluginAssembly   = 'MN_LayoutManager.dll'
$CoreAssembly     = 'MN_LayoutManager.Core.dll'
$CommandName      = 'GESTIONELAYOUT'

$RepoRoot         = Split-Path -Parent $PSScriptRoot
$SolutionPath     = Join-Path $RepoRoot 'MN_LayoutManager.sln'
$PluginProject    = Join-Path $RepoRoot 'src\MN_LayoutManager\MN_LayoutManager.csproj'
$BuildOutputDir   = Join-Path $RepoRoot 'src\MN_LayoutManager\bin\Release\net48'
$BundleDir        = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\$BundleName"
$BundleContents   = Join-Path $BundleDir 'Contents'

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Write-Ok([string]$Text) {
    Write-Host "    OK  $Text" -ForegroundColor Green
}

function Stop-WithError([string]$Text) {
    Write-Host ''
    Write-Host "!!! $Text" -ForegroundColor Red
    Write-Host ''
    Write-Host 'Premi INVIO per chiudere.'
    [void](Read-Host)
    exit 1
}

# ---------------------------------------------------------------- disinstallazione
if ($Uninstall) {
    Write-Step "Rimozione del plugin da $BundleDir"
    if (Test-Path $BundleDir) {
        Remove-Item -Recurse -Force $BundleDir
        Write-Ok 'Plugin rimosso.'
    }
    else {
        Write-Host '    Il plugin non risultava installato.' -ForegroundColor Yellow
    }
    Write-Host ''
    Write-Host 'Premi INVIO per chiudere.'
    [void](Read-Host)
    exit 0
}

# ---------------------------------------------------------------- controlli iniziali
Write-Step 'Controllo che AutoCAD sia chiuso'
$running = Get-Process -Name 'acad' -ErrorAction SilentlyContinue
if ($running) {
    Stop-WithError "AutoCAD e' aperto. Chiudilo e rilancia questo script, altrimenti i file del plugin sono bloccati e la copia fallisce."
}
Write-Ok 'AutoCAD non risulta in esecuzione.'

Write-Step 'Controllo che dotnet sia installato'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Stop-WithError "Non trovo 'dotnet'. Installa .NET SDK da https://dotnet.microsoft.com/download e riprova."
}
Write-Ok "dotnet trovato: $($dotnet.Source)"

# ---------------------------------------------------------------- test
if (-not $SkipTests) {
    Write-Step 'Esecuzione dei test automatici'
    & dotnet test $SolutionPath -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Stop-WithError 'I test automatici NON sono passati. Il plugin non e'' stato installato.'
    }
    Write-Ok 'Tutti i test sono passati.'
}
else {
    Write-Host ''
    Write-Host '    Test saltati su richiesta (-SkipTests).' -ForegroundColor Yellow
}

# ---------------------------------------------------------------- compilazione
Write-Step 'Compilazione del plugin in Release'
& dotnet build $PluginProject -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Stop-WithError 'La compilazione e'' fallita. Il plugin non e'' stato installato.'
}

$pluginPath = Join-Path $BuildOutputDir $PluginAssembly
$corePath   = Join-Path $BuildOutputDir $CoreAssembly

foreach ($file in @($pluginPath, $corePath)) {
    if (-not (Test-Path $file)) {
        Stop-WithError "La compilazione e' andata a buon fine ma non trovo il file $file."
    }
}
Write-Ok 'Compilazione completata.'

# ---------------------------------------------------------------- installazione
Write-Step "Installazione in $BundleDir"

if (Test-Path $BundleDir) {
    Remove-Item -Recurse -Force $BundleDir
}
New-Item -ItemType Directory -Force -Path $BundleContents | Out-Null

Copy-Item $pluginPath -Destination $BundleContents -Force
Copy-Item $corePath   -Destination $BundleContents -Force

# Il PackageContents.xml e' la "carta d'identita'" del plugin: dice ad AutoCAD
# quale file caricare, per quale versione, e di farlo automaticamente all'avvio.
$packageContents = @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AppVersion="1.0.0"
                    ProductCode="{8F2B41C6-9D3E-4A57-B1C8-7E5D2A9F3B60}"
                    Name="Gestione Layout"
                    Description="Palette agganciabile per gestire i layout di AutoCAD"
                    Author="MN">
  <CompanyDetails Name="MN" />
  <Components Description="AutoCAD $AutoCadYear">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="$AutoCadSeries" SeriesMax="$AutoCadSeries" />
    <ComponentEntry AppName="MN_LayoutManager"
                    Version="1.0.0"
                    ModuleName="./Contents/$PluginAssembly"
                    AppDescription="Palette Gestione Layout"
                    LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
"@

Set-Content -Path (Join-Path $BundleDir 'PackageContents.xml') -Value $packageContents -Encoding UTF8
Write-Ok 'Plugin installato.'

# ---------------------------------------------------------------- riepilogo
Write-Host ''
Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
Write-Host ' FATTO' -ForegroundColor Green
Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
Write-Host " Installato in : $BundleDir"
Write-Host " Per AutoCAD   : $AutoCadYear ($AutoCadSeries)"
Write-Host ''
Write-Host ' Cosa fare adesso:'
Write-Host "   1. apri AutoCAD $AutoCadYear"
Write-Host "   2. digita il comando  $CommandName  e premi INVIO"
Write-Host ''
Write-Host " Se qualcosa non funziona, il file di log e' qui:"
Write-Host "   $env:APPDATA\MN_LayoutManager\logs\"
Write-Host ''
Write-Host ' Per disinstallare:  .\scripts\Deploy.ps1 -Uninstall'
Write-Host ''
Write-Host 'Premi INVIO per chiudere.'
[void](Read-Host)
