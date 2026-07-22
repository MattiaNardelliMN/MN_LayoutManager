<#
.SYNOPSIS
    Crea uno ZIP distribuibile con il solo plugin gia' compilato.

.DESCRIPTION
    Serve per provare il plugin su UN ALTRO PC, dove non ci sono ne' il codice
    sorgente ne' gli strumenti di sviluppo. Fa in ordine:
      1. esegue i test e compila il plugin in Release (se qualcosa fallisce si ferma);
      2. mette insieme il bundle (DLL + PackageContents.xml);
      3. ci aggiunge un installatore da doppio click e un LEGGIMI;
      4. comprime tutto in un unico file .zip.

    Lo ZIP prodotto NON contiene codice sorgente.

.PARAMETER SkipTests
    Salta i test automatici (sconsigliato).

.PARAMETER OutputDir
    Cartella in cui salvare lo ZIP. Predefinita: <repo>\dist (non versionata).
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comune.ps1')

$RepoRoot       = Split-Path -Parent $PSScriptRoot
$SolutionPath   = Join-Path $RepoRoot 'MN_LayoutManager.sln'
$PluginProject  = Join-Path $RepoRoot 'src\MN_LayoutManager\MN_LayoutManager.csproj'
$BuildRoot      = Join-Path $RepoRoot 'src\MN_LayoutManager\bin\Release'
$TemplateDir    = Join-Path $PSScriptRoot 'pacchetto'

if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot 'dist' }

# Cartella temporanea dove si monta il pacchetto prima di comprimerlo.
$StageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("MN_LayoutManager_pkg_" + [guid]::NewGuid().ToString('N'))

try {
    # ------------------------------------------------------------ controlli
    Write-Step 'Controllo che dotnet sia installato'
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Stop-WithError "Non trovo 'dotnet'. Installa .NET SDK da https://dotnet.microsoft.com/download e riprova."
    }
    Write-Ok "dotnet trovato: $($dotnet.Source)"

    # ------------------------------------------------------------ test
    if (-not $SkipTests) {
        Write-Step 'Esecuzione dei test automatici'
        & dotnet test $SolutionPath -c Release --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Stop-WithError 'I test automatici NON sono passati. Il pacchetto non e'' stato creato.'
        }
        Write-Ok 'Tutti i test sono passati.'
    }
    else {
        Write-Host ''
        Write-Host '    Test saltati su richiesta (-SkipTests).' -ForegroundColor Yellow
    }

    # ------------------------------------------------------------ compilazione
    Write-Step 'Compilazione del plugin in Release'
    & dotnet build $PluginProject -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Stop-WithError 'La compilazione e'' fallita. Il pacchetto non e'' stato creato.'
    }

    Write-Ok 'Compilazione completata.'

    # ------------------------------------------------------------ montaggio
    Write-Step 'Preparazione del contenuto del pacchetto'

    # Stessa funzione usata da Deploy.ps1: il bundle dentro lo ZIP e' identico a
    # quello che si installa qui. I .pdb vengono esclusi (servono a chi sviluppa).
    $stageBundle = Join-Path $StageRoot $BundleName
    Copy-BundleContents -BuildRoot $BuildRoot -BundleDir $stageBundle

    foreach ($t in $Targets) {
        Write-Ok "$($t.Descrizione) -> Contents\$($t.Cartella)"
    }

    Copy-Item (Join-Path $PSScriptRoot 'Comune.ps1')            -Destination $StageRoot -Force
    Copy-Item (Join-Path $TemplateDir 'Installa.ps1')           -Destination $StageRoot -Force
    Copy-Item (Join-Path $TemplateDir 'Installa plugin.bat')    -Destination $StageRoot -Force
    Copy-Item (Join-Path $TemplateDir 'Disinstalla plugin.bat') -Destination $StageRoot -Force

    # Il LEGGIMI e' un modello con dei segnaposto: qui vengono sostituiti con i
    # dati reali di questa build, cosi' chi riceve lo ZIP sa cosa ha in mano.
    $commit = 'sconosciuta'
    try {
        $describe = & git -C $RepoRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $describe) { $commit = $describe.Trim() }
    }
    catch {
        # git non installato o cartella non versionata: non e' un problema,
        # il pacchetto resta valido, cambia solo una riga informativa.
        $commit = 'sconosciuta'
    }

    $elencoVersioni = ($Targets | ForEach-Object { "  - $($_.Descrizione)" }) -join "`r`n"

    $readme = Get-Content (Join-Path $TemplateDir 'LEGGIMI.txt') -Raw
    $readme = $readme.Replace('{ANNI}',     $AutoCadYearRange).
                      Replace('{VERSIONI}', $elencoVersioni).
                      Replace('{VERSIONE}', $PluginVersion).
                      Replace('{COMANDO}',  $CommandName).
                      Replace('{COMMIT}',   $commit).
                      Replace('{DATA}',     (Get-Date -Format 'dd/MM/yyyy'))
    Set-Content -Path (Join-Path $StageRoot 'LEGGIMI.txt') -Value $readme -Encoding UTF8

    Write-Ok 'Contenuto pronto.'

    # ------------------------------------------------------------ compressione
    Write-Step 'Creazione dello ZIP'
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

    $zipName = "MN_LayoutManager_v{0}_AutoCAD{1}_{2}.zip" -f $PluginVersion, $AutoCadYearRange, (Get-Date -Format 'yyyy-MM-dd')
    $zipPath = Join-Path $OutputDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path (Join-Path $StageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Ok 'ZIP creato.'

    # ------------------------------------------------------------ riepilogo
    $sizeKb = [math]::Round((Get-Item $zipPath).Length / 1KB, 1)
    Write-Host ''
    Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
    Write-Host ' FATTO' -ForegroundColor Green
    Write-Host '--------------------------------------------------------------' -ForegroundColor DarkGray
    Write-Host " Pacchetto : $zipPath"
    Write-Host " Dimensione: $sizeKb KB"
    Write-Host " Per       : AutoCAD $AutoCadYearRange, Windows 64 bit"
    Write-Host ''
    Write-Host ' Sull''altro PC:'
    Write-Host '   1. estrai TUTTO lo ZIP in una cartella'
    Write-Host '   2. chiudi AutoCAD'
    Write-Host '   3. doppio click su "Installa plugin.bat"'
    Write-Host "   4. apri AutoCAD e digita  $CommandName"
    Write-Host ''
}
finally {
    if (Test-Path $StageRoot) {
        Remove-Item -Recurse -Force $StageRoot -ErrorAction SilentlyContinue
    }
}
