<#
.SYNOPSIS
    Costanti e funzioni condivise fra gli script del progetto.

.DESCRIPTION
    Questo file NON si esegue da solo: viene "incluso" (dot-source) dagli altri
    script con la riga:

        . (Join-Path $PSScriptRoot 'Comune.ps1')

    Serve a tenere in UN SOLO POSTO le informazioni che altrimenti andrebbero
    ripetute in ogni script (versione di AutoCAD, nomi dei file, struttura del
    bundle). Se un giorno il plugin passa ad AutoCAD 2025, si cambia qui e tutti
    gli script si adeguano.
#>

# ---------------------------------------------------------------- costanti
$AutoCadYear      = '2024'
$AutoCadSeries    = 'R24.3'   # sigla interna di AutoCAD 2024
$PluginVersion    = '1.0.0'
$BundleName       = 'MN_LayoutManager.bundle'
$PluginAssembly   = 'MN_LayoutManager.dll'
$CoreAssembly     = 'MN_LayoutManager.Core.dll'
$CommandName      = 'GESTIONELAYOUT'
$ProductCode      = '{8F2B41C6-9D3E-4A57-B1C8-7E5D2A9F3B60}'

# Cartella in cui AutoCAD cerca da solo i plugin da caricare all'avvio.
$PluginsRoot      = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins'
$BundleDir        = Join-Path $PluginsRoot $BundleName

# ---------------------------------------------------------------- funzioni

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

<#
.SYNOPSIS
    Costruisce il contenuto del file PackageContents.xml del bundle.

.DESCRIPTION
    Il PackageContents.xml e' la "carta d'identita'" del plugin: dice ad AutoCAD
    quale DLL caricare, per quale versione del programma, e di farlo
    automaticamente all'avvio (cosi' non serve digitare NETLOAD).
#>
function New-PackageContentsXml {
    [OutputType([string])]
    param()

    return @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AppVersion="$PluginVersion"
                    ProductCode="$ProductCode"
                    Name="Gestione Layout"
                    Description="Palette agganciabile per gestire i layout di AutoCAD"
                    Author="MN">
  <CompanyDetails Name="MN" />
  <Components Description="AutoCAD $AutoCadYear">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="$AutoCadSeries" SeriesMax="$AutoCadSeries" />
    <ComponentEntry AppName="MN_LayoutManager"
                    Version="$PluginVersion"
                    ModuleName="./Contents/$PluginAssembly"
                    AppDescription="Palette Gestione Layout"
                    LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
"@
}
