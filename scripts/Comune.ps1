<#
.SYNOPSIS
    Costanti e funzioni condivise fra gli script del progetto.

.DESCRIPTION
    Questo file NON si esegue da solo: viene "incluso" (dot-source) dagli altri
    script con la riga:

        . (Join-Path $PSScriptRoot 'Comune.ps1')

    Serve a tenere in UN SOLO POSTO le informazioni che altrimenti andrebbero
    ripetute in ogni script (versioni di AutoCAD supportate, nomi dei file,
    struttura del bundle).

    PER AGGIUNGERE UNA VERSIONE DI AUTOCAD si aggiunge una riga a $Targets qui
    sotto e il corrispondente TargetFramework nel file
    src\MN_LayoutManager\MN_LayoutManager.csproj. Nient'altro.
#>

# ---------------------------------------------------------------- costanti
$BundleName       = 'MN_LayoutManager.bundle'
$PluginAssembly   = 'MN_LayoutManager.dll'
$CoreAssembly     = 'MN_LayoutManager.Core.dll'
$CommandName      = 'GESTIONELAYOUT'
# Secondo nome dello stesso comando (vedi PaletteCommandAlias in Commands.cs).
$CommandAlias     = 'LAYOUTMANAGER'
$ProductCode      = '{8F2B41C6-9D3E-4A57-B1C8-7E5D2A9F3B60}'

<#
    Le versioni di AutoCAD supportate.

    AutoCAD ha cambiato motore .NET due volte, quindi lo STESSO codice va
    compilato tre volte e il bundle le contiene tutte e tre: all'avvio AutoCAD
    legge il PackageContents.xml, riconosce la propria sigla (la "serie") e
    carica solo la cartella che gli compete.

      Tfm       = come si chiama il target per il compilatore
      Cartella  = sottocartella dentro Contents\ (nome breve, senza "-windows")
      SerieMin/Max = sigle interne di AutoCAD coperte da questa compilazione
#>
$Targets = @(
    [pscustomobject]@{
        Tfm         = 'net48'
        Cartella    = 'net48'
        SerieMin    = 'R24.3'
        SerieMax    = 'R24.3'
        Anni        = '2024'
        Descrizione = 'AutoCAD 2024 (.NET Framework 4.8)'
    }
    [pscustomobject]@{
        Tfm         = 'net8.0-windows'
        Cartella    = 'net8.0'
        SerieMin    = 'R25.0'
        SerieMax    = 'R25.1'
        Anni        = '2025-2026'
        Descrizione = 'AutoCAD 2025 e 2026 (.NET 8)'
    }
    [pscustomobject]@{
        Tfm         = 'net10.0-windows'
        Cartella    = 'net10.0'
        SerieMin    = 'R26.0'
        SerieMax    = 'R26.0'
        Anni        = '2027'
        Descrizione = 'AutoCAD 2027 (.NET 10)'
    }
)

# Estremi dell'intervallo supportato, ricavati dalla tabella: servono solo per i
# messaggi a video e per il nome dello ZIP.
$AutoCadYearMin   = ($Targets[0].Anni -split '-')[0]
$AutoCadYearMax   = ($Targets[-1].Anni -split '-')[-1]
$AutoCadYearRange = "$AutoCadYearMin-$AutoCadYearMax"

# Cartella in cui AutoCAD cerca da solo i plugin da caricare all'avvio.
# E' la stessa per tutte le versioni di AutoCAD: per questo un solo bundle basta.
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

<#
.SYNOPSIS
    Legge la versione del plugin da Directory.Build.props.

.DESCRIPTION
    La versione e' scritta una volta sola nel file di progetto, cosi' la DLL
    compilata e il PackageContents.xml dichiarano per forza la stessa cosa.

    Nota: questa funzione serve solo quando si COSTRUISCE il pacchetto. La copia
    di Comune.ps1 che finisce dentro lo ZIP non la chiama mai, perche' li' il
    bundle e' gia' pronto.
#>
function Get-PluginVersion {
    [OutputType([string])]
    param()

    $propsPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Build.props'

    if (-not (Test-Path $propsPath)) {
        Stop-WithError "Non trovo $propsPath, da cui si legge la versione del plugin."
    }

    $match = [regex]::Match((Get-Content $propsPath -Raw), '<Version>\s*([^<\s]+)\s*</Version>')
    if (-not $match.Success) {
        Stop-WithError "In $propsPath manca l'elemento <Version>: non so che versione dichiarare."
    }

    return $match.Groups[1].Value
}

<#
.SYNOPSIS
    Dice se fra gli argomenti di PowerShell c'e' -NonInteractive.

.DESCRIPTION
    E' la parte "che ragiona" del controllo sull'interattivita', tenuta separata
    apposta: non guarda com'e' fatta la sessione in corso, quindi si puo' provare
    con argomenti finti e il test vale sempre. La funzione che invece interroga
    Windows e' Test-Interattivo, qui sotto.

    PowerShell accetta i parametri abbreviati, quindi non basta cercare la parola
    intera: "-noni" vale come "-NonInteractive". Si cerca il prefisso "-non", che
    e' il piu' corto che non si confonde con nessun altro parametro. Fermarsi a
    "-no" sarebbe un guaio serio: i .bat del progetto lanciano PowerShell con
    -NoProfile, quindi la pausa sparirebbe da tutti i doppi click e la finestra
    si chiuderebbe in faccia all'utente.

.PARAMETER CommandLineArgs
    Gli argomenti da esaminare.

.OUTPUTS
    [bool] Vero se gli argomenti dichiarano una sessione senza input.
#>
function Test-ArgomentiNonInteractive {
    [OutputType([bool])]
    param(
        [string[]]$CommandLineArgs
    )

    foreach ($arg in $CommandLineArgs) {
        if ($arg -match '^-{1,2}non') {
            return $true
        }
    }

    return $false
}

<#
.SYNOPSIS
    Dice se c'e' davvero una persona davanti allo schermo che puo' premere INVIO.

.DESCRIPTION
    Serve a decidere se ha senso fermarsi con "Premi INVIO per chiudere".
    Col doppio click sul .bat la pausa e' indispensabile: senza, la finestra si
    chiuderebbe prima che si riesca a leggere l'esito. Lanciato da un altro
    script o da un'automazione, invece, non c'e' nessuno che possa rispondere:
    Read-Host solleva un errore e lo script muore PRIMA di arrivare al proprio
    exit, quindi il codice di uscita smette di distinguere "riuscito" da
    "fallito".

    Due controlli, perche' coprono casi diversi:
      - UserInteractive e' falso quando non c'e' proprio una scrivania
        (servizio di Windows, operazione pianificata);
      - -NonInteractive e' il caso piu' comune: la scrivania c'e', ma quella
        specifica sessione di PowerShell ha l'input chiuso.

.PARAMETER CommandLineArgs
    Argomenti da esaminare. Esiste per poter provare la funzione con valori
    finti: senza, si potrebbe verificare solo il caso in cui si sta girando.

.OUTPUTS
    [bool] Vero se ci si puo' aspettare una risposta dall'utente.
#>
function Test-Interattivo {
    [OutputType([bool])]
    param(
        [string[]]$CommandLineArgs = [Environment]::GetCommandLineArgs()
    )

    if (-not [Environment]::UserInteractive) {
        return $false
    }

    return -not (Test-ArgomentiNonInteractive -CommandLineArgs $CommandLineArgs)
}

<#
.SYNOPSIS
    Si ferma ad aspettare INVIO, ma solo se qualcuno puo' premerlo.

.PARAMETER Text
    Testo dell'invito. Ha un valore predefinito perche' e' sempre lo stesso.
#>
function Wait-Invio {
    param(
        [string]$Text = 'Premi INVIO per chiudere.'
    )

    if (-not (Test-Interattivo)) {
        return
    }

    Write-Host ''
    Write-Host $Text

    # Ultima rete: se l'host non ha input nonostante i controlli, la pausa e'
    # una cortesia, non il lavoro dello script. L'esito e' gia' stato scritto
    # qui sopra, quindi si prosegue verso l'exit invece di morire qui.
    try {
        [void](Read-Host)
    }
    catch {
        Write-Verbose "Pausa saltata: nessun input disponibile ($($_.Exception.Message))"
    }
}

function Stop-WithError([string]$Text) {
    Write-Host ''
    Write-Host "!!! $Text" -ForegroundColor Red
    Wait-Invio
    exit 1
}

<#
.SYNOPSIS
    Costruisce il contenuto del file PackageContents.xml del bundle.

.DESCRIPTION
    Il PackageContents.xml e' la "carta d'identita'" del plugin: dice ad AutoCAD
    quale DLL caricare, per quale versione del programma, e di farlo
    automaticamente all'avvio (cosi' non serve digitare NETLOAD).

    Contiene una voce per ogni versione supportata. Il blocco RuntimeRequirements
    sta DENTRO ogni ComponentEntry: e' quello che permette ad AutoCAD di scegliere
    la compilazione giusta per se' e ignorare le altre.

    SeriesMax va sempre indicato: senza, una versione futura di AutoCAD proverebbe
    a caricare una DLL fatta per un motore che non usa piu'.
#>
function New-PackageContentsXml {
    [OutputType([string])]
    param()

    $PluginVersion = Get-PluginVersion

    $entries = foreach ($t in $Targets) {
        @"
    <ComponentEntry AppName="MN_LayoutManager"
                    Version="$PluginVersion"
                    ModuleName="./Contents/$($t.Cartella)/$PluginAssembly"
                    AppType=".Net"
                    AppDescription="Palette Gestione Layout - $($t.Descrizione)"
                    LoadOnAutoCADStartup="True">
      <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="$($t.SerieMin)" SeriesMax="$($t.SerieMax)" />
    </ComponentEntry>
"@
    }

    return @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AppVersion="$PluginVersion"
                    ProductCode="$ProductCode"
                    Name="Gestione Layout"
                    Description="Palette agganciabile per gestire i layout di AutoCAD"
                    Author="MN">
  <CompanyDetails Name="MN" />
  <Components Description="AutoCAD $AutoCadYearRange">
$($entries -join "`r`n")
  </Components>
</ApplicationPackage>
"@
}

<#
.SYNOPSIS
    Copia dentro il bundle le DLL compilate per tutte le versioni supportate.

.PARAMETER BuildRoot
    Cartella bin\<configurazione> del progetto plugin: contiene una sottocartella
    per ogni TargetFramework.

.PARAMETER BundleDir
    Cartella del bundle da riempire. Viene svuotata prima di copiare.

.DESCRIPTION
    Si ferma con un errore se manca anche una sola delle compilazioni attese: un
    bundle a cui manca una versione non da' nessun segnale, semplicemente su quel
    AutoCAD il plugin non compare.

    Vengono copiati tutti i file prodotti tranne i .pdb (servono a chi sviluppa,
    non a chi usa il plugin). Il "tutti i file" non e' pigrizia: le compilazioni
    per .NET 8 e 10 si portano dietro file di supporto che vanno inclusi.
#>
function Copy-BundleContents {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$BuildRoot,
        [Parameter(Mandatory = $true)][string]$BundleDir
    )

    if (Test-Path $BundleDir) {
        Remove-Item -Recurse -Force $BundleDir
    }

    foreach ($t in $Targets) {
        $sorgente = Join-Path $BuildRoot $t.Tfm

        if (-not (Test-Path $sorgente)) {
            Stop-WithError "Manca la compilazione per $($t.Descrizione): non trovo la cartella $sorgente."
        }

        foreach ($atteso in @($PluginAssembly, $CoreAssembly)) {
            if (-not (Test-Path (Join-Path $sorgente $atteso))) {
                Stop-WithError "Manca il file $atteso nella compilazione per $($t.Descrizione) ($sorgente)."
            }
        }

        $destinazione = Join-Path (Join-Path $BundleDir 'Contents') $t.Cartella
        New-Item -ItemType Directory -Force -Path $destinazione | Out-Null

        Copy-Item -Path (Join-Path $sorgente '*') -Destination $destinazione -Recurse -Force

        # I .pdb si tolgono DOPO la copia invece di usare -Exclude: con -Recurse
        # quel parametro filtra solo il primo livello e i file annidati passerebbero.
        Get-ChildItem -Path $destinazione -Filter '*.pdb' -Recurse -File |
            Remove-Item -Force
    }

    New-PackageContentsXml |
        Set-Content -Path (Join-Path $BundleDir 'PackageContents.xml') -Encoding UTF8
}
