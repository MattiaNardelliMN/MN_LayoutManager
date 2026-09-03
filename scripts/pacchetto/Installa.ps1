<#
.SYNOPSIS
    Installa il plugin "Gestione Layout" gia' compilato (nessun compilatore richiesto).

.DESCRIPTION
    Questo script vive DENTRO il pacchetto ZIP distribuito, non nel repository di
    sviluppo. Non compila niente: si limita a copiare il bundle gia' pronto in
        %AppData%\Autodesk\ApplicationPlugins\
    che e' la cartella dove AutoCAD cerca da solo i plugin da caricare all'avvio.

    IMPORTANTE: AutoCAD deve essere CHIUSO, altrimenti i file risultano bloccati
    e la copia fallisce.

.PARAMETER Uninstall
    Rimuove il plugin invece di installarlo.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

# Costanti e funzioni condivise (Comune.ps1 viene copiato dentro il pacchetto).
. (Join-Path $PSScriptRoot 'Comune.ps1')

$SourceBundle = Join-Path $PSScriptRoot $BundleName

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
    Wait-Invio
    exit 0
}

# ---------------------------------------------------------------- controlli
Write-Step 'Controllo che AutoCAD sia chiuso'
$running = Get-Process -Name 'acad' -ErrorAction SilentlyContinue
if ($running) {
    Stop-WithError "AutoCAD e' aperto. Chiudilo e rilancia questo file, altrimenti i file del plugin sono bloccati e la copia fallisce."
}
Write-Ok 'AutoCAD non risulta in esecuzione.'

Write-Step 'Controllo del contenuto del pacchetto'
if (-not (Test-Path $SourceBundle)) {
    Stop-WithError "Non trovo la cartella '$BundleName' accanto a questo script. Hai estratto TUTTO il contenuto dello ZIP in una cartella prima di lanciarlo?"
}
# Il bundle contiene una cartella per ogni versione di AutoCAD supportata:
# devono esserci tutte, altrimenti su una delle versioni il plugin non
# comparirebbe e nessuno saprebbe perche'.
foreach ($t in $Targets) {
    foreach ($file in @($PluginAssembly, $CoreAssembly)) {
        $path = Join-Path $SourceBundle "Contents\$($t.Cartella)\$file"
        if (-not (Test-Path $path)) {
            Stop-WithError "Il pacchetto e' incompleto: manca $file per $($t.Descrizione). Riestrai lo ZIP e riprova."
        }
    }
}
Write-Ok 'Pacchetto completo.'

# Windows marca come "bloccati" i file provenienti da internet: se non si toglie
# quel marchio, .NET rifiuta di caricare la DLL e il plugin non parte.
Write-Step 'Sblocco dei file scaricati da internet'
Get-ChildItem -Path $SourceBundle -Recurse -File | Unblock-File
Write-Ok 'File sbloccati.'

# ---------------------------------------------------------------- installazione
Write-Step "Installazione in $BundleDir"
if (Test-Path $BundleDir) {
    Remove-Item -Recurse -Force $BundleDir
}
New-Item -ItemType Directory -Force -Path $PluginsRoot | Out-Null
Copy-Item -Path $SourceBundle -Destination $PluginsRoot -Recurse -Force
Write-Ok 'Plugin installato.'

# ---------------------------------------------------------------- riepilogo
Write-Host ''
Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
Write-Host ' FATTO' -ForegroundColor Green
Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
Write-Host " Installato in : $BundleDir"
Write-Host " Per AutoCAD   : $AutoCadYearRange (una sola installazione le copre tutte)"
Write-Host ''
Write-Host ' Cosa fare adesso:'
Write-Host "   1. apri AutoCAD (dal $AutoCadYearMin al $AutoCadYearMax)"
Write-Host "   2. digita il comando  $CommandName  (oppure  $CommandAlias)  e premi INVIO"
Write-Host ''
Write-Host " Se qualcosa non funziona, il file di log e' qui:"
Write-Host "   $env:APPDATA\MN_LayoutManager\logs\"
Write-Host ''
Write-Host ' Per disinstallare: doppio click su "Disinstalla plugin.bat"'
Wait-Invio
