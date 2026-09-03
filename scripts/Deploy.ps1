<#
.SYNOPSIS
    Compila il plugin "Gestione Layout" e lo installa per AutoCAD 2024-2027.

.DESCRIPTION
    Fa tre cose, in quest'ordine:
      1. compila il progetto in Release ed esegue i test;
      2. se qualcosa fallisce SI FERMA e non installa niente;
      3. copia il plugin in un "bundle" dentro
         %AppData%\Autodesk\ApplicationPlugins\, cosi' AutoCAD lo carica da solo
         a ogni avvio, senza dover digitare NETLOAD.

    Il bundle contiene tutte le versioni compilate insieme: la stessa
    installazione vale per AutoCAD 2024, 2025, 2026 e 2027, e ogni AutoCAD
    carica da solo quella giusta per se'.

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
# Nomi, versione di AutoCAD e funzioni di stampa stanno in Comune.ps1, cosi'
# sono definiti una volta sola e valgono anche per CreaPacchetto.ps1.
. (Join-Path $PSScriptRoot 'Comune.ps1')

$RepoRoot         = Split-Path -Parent $PSScriptRoot
$SolutionPath     = Join-Path $RepoRoot 'MN_LayoutManager.sln'
$PluginProject    = Join-Path $RepoRoot 'src\MN_LayoutManager\MN_LayoutManager.csproj'

# Dentro questa cartella c'e' una sottocartella per ogni versione di AutoCAD
# supportata (net48, net8.0-windows, ...): le mette li' il compilatore.
$BuildRoot        = Join-Path $RepoRoot 'src\MN_LayoutManager\bin\Release'

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

Write-Ok 'Compilazione completata.'

# ---------------------------------------------------------------- installazione
Write-Step "Installazione in $BundleDir"

# Copy-BundleContents controlla da sola che ci siano TUTTE le compilazioni
# attese e si ferma con un messaggio chiaro se ne manca una.
Copy-BundleContents -BuildRoot $BuildRoot -BundleDir $BundleDir

foreach ($t in $Targets) {
    Write-Ok "$($t.Descrizione) -> Contents\$($t.Cartella)"
}
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
Write-Host ' Per disinstallare:  .\scripts\Deploy.ps1 -Uninstall'
Wait-Invio
